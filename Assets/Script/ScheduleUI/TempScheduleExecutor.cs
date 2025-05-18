using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class TempScheduleExecutor : MonoBehaviour
{
    [Header("연결 필수 (Inspector에서 할당)")]
    public ScheduleDropZone scheduleQueuePanel; // 스케줄 아이템들이 배치되는 패널
    public IdolCharacter idolCharacter;      // 스케줄 효과를 받을 아이돌
    public UIManager uiManager; 



    [Header("실행 설정")]
    [Tooltip("각 스케줄 '묶음' 또는 단일 스케줄 실행 후, 다음 스케줄 실행까지의 대기 시간 (초)")]
    public float delayBetweenSchedules = 1.5f;
    [Tooltip("아이템 제거 후 나머지 아이템이 올라오는 애니메이션과 다음 스케줄 실행 사이의 추가 딜레이 (선택 사항)")]
    public float delayAfterLayoutAnimation = 0.3f; // RefreshLayout 애니메이션 시간 고려

    [Header("선택 사항 (버튼 UI 사용할 경우)")]
    public Button executeButton;

    private PlayerInputActions playerInputActions;
    private Coroutine F_ProcessQueueCoroutine = null;
    private NextScheduleModifiers pendingModifiersForNextSchedule;

    void Awake()
    {
        playerInputActions = new PlayerInputActions();
        pendingModifiersForNextSchedule = new NextScheduleModifiers();

        if (scheduleQueuePanel == null) Debug.LogError("TempScheduleExecutor: ScheduleQueuePanel이 Inspector에 할당되지 않았습니다!");
        if (idolCharacter == null) Debug.LogError("TempScheduleExecutor: IdolCharacter가 Inspector에 할당되지 않았습니다!");
    }

    void OnEnable()
    {
        if (playerInputActions.Gameplay.Get() != null)
        {
            playerInputActions.Gameplay.Enable();
            if (playerInputActions.Gameplay.ExecuteSchedule != null)
            {
                playerInputActions.Gameplay.ExecuteSchedule.performed += OnExecuteScheduleInput;
            }
            else Debug.LogError("TempScheduleExecutor: 'ExecuteSchedule' Action을 'Gameplay' Action Map에서 찾을 수 없습니다.");
        }
        else Debug.LogError("TempScheduleExecutor: 'Gameplay' Action Map을 찾을 수 없습니다.");

        if (executeButton != null)
            executeButton.onClick.AddListener(StartProcessingQueue);
    }

    void OnDisable()
    {
        if (playerInputActions.Gameplay.Get() != null && playerInputActions.Gameplay.ExecuteSchedule != null)
        {
            playerInputActions.Gameplay.ExecuteSchedule.performed -= OnExecuteScheduleInput;
            playerInputActions.Gameplay.Disable();
        }
        if (executeButton != null)
            executeButton.onClick.RemoveListener(StartProcessingQueue);
    }

    private void OnExecuteScheduleInput(InputAction.CallbackContext context)
    {
        Debug.Log("스케줄 큐 실행 입력 감지!");
        StartProcessingQueue();
    }

    public void StartProcessingQueue()
    {
        if (F_ProcessQueueCoroutine != null)
        {
            Debug.LogWarning("이미 스케줄 큐를 처리 중입니다.");
            return;
        }
        if (scheduleQueuePanel == null || idolCharacter == null)
        {
            Debug.LogWarning("실행에 필요한 요소(패널 또는 아이돌)가 설정되지 않았습니다.");
            return;
        }
        if (scheduleQueuePanel.transform.childCount == 0)
        {
            Debug.Log("실행할 스케줄이 큐에 없습니다.");
            return;
        }
        F_ProcessQueueCoroutine = StartCoroutine(ProcessScheduleQueueCoroutine());
    }

    IEnumerator ProcessScheduleQueueCoroutine()
    {
        Debug.Log("스케줄 큐 자동 실행 시작...");
        if (executeButton != null) executeButton.interactable = false;

        while (scheduleQueuePanel.transform.childCount > 0)
        {
            DraggableScheduleItem firstScheduleItemInQueue = scheduleQueuePanel.transform.GetChild(0).GetComponent<DraggableScheduleItem>();
            if (firstScheduleItemInQueue == null || firstScheduleItemInQueue.scheduleData == null)
            {
                Debug.LogWarning("큐의 첫 번째 아이템이 유효하지 않습니다. 제거합니다.");
                if (scheduleQueuePanel.transform.childCount > 0) Destroy(scheduleQueuePanel.transform.GetChild(0).gameObject);
                yield return null; // Destroy 반영 대기
                continue;
            }

            ScheduleData currentScheduleData = firstScheduleItemInQueue.scheduleData;
            int bundleSizeN = 0;
            List<Transform> itemsInBundle = new List<Transform>();

            for (int i = 0; i < scheduleQueuePanel.transform.childCount; i++)
            {
                Transform itemTransform = scheduleQueuePanel.transform.GetChild(i);
                DraggableScheduleItem ditem = itemTransform.GetComponent<DraggableScheduleItem>();
                if (ditem != null && ditem.scheduleData == currentScheduleData)
                {
                    itemsInBundle.Add(itemTransform);
                    bundleSizeN++;
                }
                else
                {
                    break;
                }
            }

            if (bundleSizeN > 0)
            {
                Debug.Log($"'{currentScheduleData.scheduleName}' 스케줄 묶음 (크기: {bundleSizeN}) 처리 시작.");
                PerformScheduleBundle(currentScheduleData, bundleSizeN, pendingModifiersForNextSchedule);
                pendingModifiersForNextSchedule.Reset();

                foreach (Transform itemToRemove in itemsInBundle)
                {
                    if (itemToRemove != null) Destroy(itemToRemove.gameObject);
                }

                // 중요: Destroy 호출 후, 실제 파괴가 반영될 때까지 한 프레임 대기합니다.
                yield return null;
                // 또는 yield return new WaitForEndOfFrame(); // 프레임의 끝까지 대기

                // 아이템 제거 후, 남아있는 아이템들의 레이아웃을 애니메이션으로 갱신합니다.
                if (scheduleQueuePanel.transform.childCount > 0) // 남아있는 아이템이 있을 때만
                {
                    Debug.Log("남아있는 스케줄들 위로 올리는 애니메이션 시작.");
                    scheduleQueuePanel.RefreshLayout(true,true); // animate: true //UseTweem = true 
                    // RefreshLayout 내부의 애니메이션 시간(siblingAnimationDuration)만큼 기다려주는 것이 좋음
                    // 또는, RefreshLayout이 코루틴이 아니라면, 여기서 그 시간만큼 대기
                    yield return new WaitForSeconds(scheduleQueuePanel.siblingAnimationDuration + delayAfterLayoutAnimation);
                }


                // 다음 스케줄에 대한 연계 효과 준비
                if (scheduleQueuePanel.transform.childCount > 0)
                {
                    DraggableScheduleItem nextScheduleItem = scheduleQueuePanel.transform.GetChild(0).GetComponent<DraggableScheduleItem>();
                    if (nextScheduleItem != null && nextScheduleItem.scheduleData != null)
                    {
                        ScheduleData nextScheduleInQueueData = nextScheduleItem.scheduleData;
                        if (currentScheduleData.outgoingLinkEffectRules != null)
                        {
                            foreach (var rule in currentScheduleData.outgoingLinkEffectRules)
                            {
                                if (rule.targetNextScheduleCondition == nextScheduleInQueueData && rule.effectToApplyOnNextSchedule != null)
                                {
                                    rule.effectToApplyOnNextSchedule.ApplyToNext(idolCharacter, currentScheduleData, nextScheduleInQueueData, pendingModifiersForNextSchedule);
                                }
                            }
                        }
                    }
                }
            }

            // 다음 묶음/아이템 실행 전 대기 (이 대기는 연계 효과 준비 후, 다음 루프 시작 전)
            if (scheduleQueuePanel.transform.childCount > 0)
            {
                // delayBetweenSchedules는 이미 RefreshLayout 애니메이션 이후의 간격을 의미하게 됨
                // 만약 RefreshLayout 애니메이션 시간과 별도로 간격을 주고 싶다면,
                // 위의 WaitForSeconds(scheduleQueuePanel.siblingAnimationDuration) 와 여기서의 delayBetweenSchedules를 조절.
                // 현재는 RefreshLayout 애니메이션 후 추가 delayAfterLayoutAnimation, 그 후 다음 루프 시작 전 delayBetweenSchedules
                yield return new WaitForSeconds(delayBetweenSchedules);
            }
        }

        Debug.Log("모든 스케줄 실행 완료.");
        if (executeButton != null) executeButton.interactable = true;
        F_ProcessQueueCoroutine = null;
        uiManager.ShowMainMenuPanel();
        ClearSchedule();
        SpawnNewScheduleItemTest(0);
        SpawnNewScheduleItemTest(0);
        SpawnNewScheduleItemTest(1);
        SpawnNewScheduleItemTest(1);
        SpawnNewScheduleItemTest(2);
        //uiManager.CurrentTurnUpdate();
    }

    private void PerformScheduleBundle(ScheduleData dataToExecute, int bundleSizeN, NextScheduleModifiers modifiers)
    {
        // ... (이 함수 내용은 이전과 동일하게 유지) ...
        Debug.Log($"--- '{dataToExecute.scheduleName}' (묶음 크기: {bundleSizeN}) 실행 시작 ---");

        float finalSuccessRate = dataToExecute.baseSuccessRate;
        int baseStatImprovementForBundle = dataToExecute.primaryStatImprovementAmount * bundleSizeN;
        int finalStatImprovement = baseStatImprovementForBundle;

        int finalStressChangeOnSuccess = dataToExecute.stressChangeOnSuccess;
        int finalStressChangeOnFailure = dataToExecute.stressChangeOnFailure;

        if (modifiers != null)
        {
            if (modifiers.successRateBonus != 0)
            {
                finalSuccessRate += modifiers.successRateBonus;
                Debug.Log($"연계 효과로 성공 확률 +{modifiers.successRateBonus * 100:F0}% 적용됨.");
            }
            if (modifiers.statToBuff == dataToExecute.primaryTargetStat && modifiers.statBuffAmount != 0)
            {
                finalStatImprovement += modifiers.statBuffAmount;
                Debug.Log($"연계 효과로 {modifiers.statToBuff} 스탯 +{modifiers.statBuffAmount} 추가 적용됨.");
            }
        }

        if (dataToExecute.canFormConsecutiveBundle && bundleSizeN >= dataToExecute.minItemsForConsecutiveEffect)
        {
            Debug.Log($"'{dataToExecute.scheduleName}' 연속 효과 발동! (묶음 크기: {bundleSizeN})");
            if (dataToExecute.consecutiveSuccessRateModifierPerN != 0)
            {
                int nForPenaltyCalc = bundleSizeN;
                if (dataToExecute.maxNForSuccessRatePenaltyStack > 0 && dataToExecute.consecutiveSuccessRateModifierPerN < 0)
                {
                    nForPenaltyCalc = Mathf.Min(bundleSizeN, dataToExecute.maxNForSuccessRatePenaltyStack);
                }
                float successRateChange = dataToExecute.consecutiveSuccessRateModifierPerN * nForPenaltyCalc;
                finalSuccessRate += successRateChange;
                Debug.Log($"연속 효과로 성공 확률 보정: {successRateChange * 100:F0}%");
            }
            if (dataToExecute.consecutiveStatBonus != 0)
            {
                if (dataToExecute.applyStatBonusPerItemInBundle)
                {
                    finalStatImprovement += (dataToExecute.consecutiveStatBonus * bundleSizeN);
                }
                else
                {
                    finalStatImprovement += dataToExecute.consecutiveStatBonus;
                }
                Debug.Log($"연속 효과 스탯 보너스 적용됨.");
            }
        }
        finalSuccessRate = Mathf.Clamp01(finalSuccessRate);

        bool success = Random.value < finalSuccessRate;
        Debug.Log($"최종 성공 확률: {finalSuccessRate * 100:F0}% > 결과: {(success ? "성공!" : "실패...")}");
        string effectLog = "";
        if (success)
        {
            effectLog += "성공 효과: ";
            if (dataToExecute.primaryTargetStat != StatType.None && finalStatImprovement != 0)
            {
                switch (dataToExecute.primaryTargetStat)
                {
                    case StatType.Vocal: idolCharacter.AddVocalPoint(finalStatImprovement); break;
                    case StatType.Dance: idolCharacter.AddDancePoint(finalStatImprovement); break;
                    case StatType.Rap: idolCharacter.AddRapPoint(finalStatImprovement); break;
                    default: Debug.LogWarning($"PerformScheduleBundle: 처리되지 않은 primaryTargetStat - {dataToExecute.primaryTargetStat}에 대한 스탯 적용 로직이 없습니다."); break;
                }
                effectLog += $"{dataToExecute.primaryTargetStat} +{finalStatImprovement}, ";
            }
            idolCharacter.ChangeStress(finalStressChangeOnSuccess);
            effectLog += $"스트레스 {(finalStressChangeOnSuccess >= 0 ? "+" : "") + finalStressChangeOnSuccess}";
        }
        else
        {
            effectLog += "실패 효과: ";
            idolCharacter.ChangeStress(finalStressChangeOnFailure);
            effectLog += $"스트레스 {(finalStressChangeOnFailure >= 0 ? "+" : "") + finalStressChangeOnFailure}";
        }
        Debug.Log(effectLog.TrimEnd(' ', ','));
        Debug.Log($"'{dataToExecute.scheduleName}' (묶음 크기: {bundleSizeN}) 실행 완료. 현재 아이돌 상태: {idolCharacter.GetCurrentStatus()}");
        Debug.Log("------------------------------------");
    }
   
    public List<GameObject> scheduleItemPrefab;
    public Transform availableSchedulesPanel; // Inspector에서 할당할 부모 패널

    public void ClearSchedule()
    {
        for (int i = 0; i < availableSchedulesPanel.childCount; i++)
        {
            Destroy(availableSchedulesPanel.GetChild(i).gameObject);
           
            
        }
    }
    public void SpawnNewScheduleItemTest(int i)
    {
        if (scheduleItemPrefab == null || availableSchedulesPanel == null)
        {
            Debug.LogError("스케줄 프리팹 또는 부모 패널이 할당되지 않았습니다!");
            return;
        }
           // 1. 프리팹을 사용하여 새 스케줄 아이템 게임 오브젝트를 생성합니다.
        // Instantiate의 두 번째 파라미터로 부모 Transform을 지정하면 바로 자식으로 설정됩니다.
        GameObject newScheduleObject = Instantiate(scheduleItemPrefab[i], availableSchedulesPanel);
    }
}
