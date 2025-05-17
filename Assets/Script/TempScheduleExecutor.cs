using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class TempScheduleExecutor : MonoBehaviour
{
    [Header("연결 필수 (Inspector에서 할당)")]
    public ScheduleDropZone scheduleQueuePanel;
    public IdolCharacter idolCharacter;

    [Header("실행 설정")]
    public float delayBetweenSchedules = 1.5f;

    [Header("선택 사항 (버튼 UI 사용할 경우)")]
    public Button executeButton;

    private PlayerInputActions playerInputActions;
    private Coroutine F_ProcessQueueCoroutine = null;

    // 다음에 실행될 스케줄에 적용될 임시 보정 값들을 저장하는 객체
    private NextScheduleModifiers pendingModifiersForNextSchedule;
    void Awake()
    {
        pendingModifiersForNextSchedule = new NextScheduleModifiers();
        playerInputActions = new PlayerInputActions();
        if (scheduleQueuePanel == null) Debug.LogError("TempScheduleExecutor: ScheduleQueuePanel이 Inspector에 할당되지 않았습니다!");
        if (idolCharacter == null) Debug.LogError("TempScheduleExecutor: IdolCharacter가 Inspector에 할당되지 않았습니다!");
    }

    void OnEnable()
    {
        // Input Actions 에셋의 실제 Action Map 이름을 사용 (예: "Gameplay")
        if (playerInputActions.ExecuteSchedule.Get() != null)
        {
            playerInputActions.ExecuteSchedule.Enable();
            if (playerInputActions.ExecuteSchedule.start != null)
            {
                playerInputActions.ExecuteSchedule.start.performed += OnExecuteScheduleInput;
            }
            else Debug.LogError("TempScheduleExecutor: 'ExecuteSchedule' Action을 'Gameplay' Action Map에서 찾을 수 없습니다.");
        }
        else Debug.LogError("TempScheduleExecutor: 'Gameplay' Action Map을 찾을 수 없습니다. Input Actions 에셋 설정을 확인해주세요.");

        if (executeButton != null)
            executeButton.onClick.AddListener(StartProcessingQueue);
    }

    void OnDisable()
    {
        if (playerInputActions.ExecuteSchedule.Get() != null && playerInputActions.ExecuteSchedule.start != null)
        {
            playerInputActions.ExecuteSchedule.start.performed -= OnExecuteScheduleInput;
            playerInputActions.ExecuteSchedule.Disable();
        }

        if (executeButton != null)
            executeButton.onClick.RemoveListener(StartProcessingQueue);
    }

    private void OnExecuteScheduleInput(InputAction.CallbackContext context)
    {
        Debug.Log("스케줄 큐 실행 입력 감지! (New Input System)");
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
            Transform firstItemTransform = scheduleQueuePanel.transform.GetChild(0);
            DraggableScheduleItem firstScheduleItemInQueue = firstItemTransform.GetComponent<DraggableScheduleItem>();

            if (firstScheduleItemInQueue == null || firstScheduleItemInQueue.scheduleData == null)
            {
                Debug.LogWarning("큐의 첫 번째 아이템이 유효하지 않습니다. 제거합니다.");
                if (scheduleQueuePanel.transform.childCount > 0) Destroy(scheduleQueuePanel.transform.GetChild(0).gameObject);
                continue; // 다음 루프로
            }

            ScheduleData currentScheduleData = firstScheduleItemInQueue.scheduleData;
            int bundleSizeN = 0;
            List<Transform> itemsInBundle = new List<Transform>();

            for (int i = 0; i < scheduleQueuePanel.transform.childCount; i++)
            {
                Transform itemTransform = scheduleQueuePanel.transform.GetChild(i);
                DraggableScheduleItem ditem = itemTransform.GetComponent<DraggableScheduleItem>();
                if (ditem != null && ditem.scheduleData != null && ditem.scheduleData == currentScheduleData)
                {
                    itemsInBundle.Add(itemTransform);
                    bundleSizeN++;
                }
                else
                {
                    // 현재 아이템과 다른 ScheduleData를 가진 아이템이 발견되면 루프 종료
                    break;
                }
            }


            // 2. 식별된 묶음을 실행합니다.
            if (bundleSizeN > 0)
            {
                Debug.Log($"'{currentScheduleData.scheduleName}' 스케줄 묶음 (크기: {bundleSizeN}) 처리 시작.");
                PerformScheduleBundle(currentScheduleData, bundleSizeN, pendingModifiersForNextSchedule);
                pendingModifiersForNextSchedule.Reset(); // 다음 스케줄을 위해 모디파이어 초기화

                // 실행된 묶음에 포함된 모든 UI 아이템들을 큐에서 제거합니다.
                foreach (Transform itemToRemove in itemsInBundle)
                {
                    
                    Destroy(itemToRemove.gameObject);
                }
            }
            yield return null;


            // --- 연계 효과 발동 준비 ---
            if (scheduleQueuePanel.transform.childCount > 0) // 다음에 실행할 스케줄이 있다면
            {
                DraggableScheduleItem nextScheduleItem = scheduleQueuePanel.transform.GetChild(0).GetComponent<DraggableScheduleItem>();
                if (nextScheduleItem != null && nextScheduleItem.scheduleData != null)
                {
                    ScheduleData nextScheduleInQueueData = nextScheduleItem.scheduleData;
                    // currentScheduleData의 발신 연계 효과 규칙들을 확인
                    if (currentScheduleData.outgoingLinkEffectRules != null)
                    {
                        foreach (var rule in currentScheduleData.outgoingLinkEffectRules)
                        {
                            // 다음 스케줄이 규칙의 조건과 일치하고, 적용할 효과가 있다면
                            
                           
                             if (rule.targetNextScheduleCondition == nextScheduleInQueueData && rule.effectToApplyOnNextSchedule != null)
                            {
                                Debug.Log($"'{currentScheduleData.scheduleName}'가 다음 스케줄 '{nextScheduleInQueueData.scheduleName}'에 연계 효과를 준비합니다: {rule.effectToApplyOnNextSchedule.effectDescription}");
                                // pendingModifiersForNextSchedule는 이미 위에서 Reset() 되었으므로, 여기에 새 효과를 누적
                                rule.effectToApplyOnNextSchedule.ApplyToNext(idolCharacter, currentScheduleData, nextScheduleInQueueData, pendingModifiersForNextSchedule);
                            }
                        }
                        
                    }
                    
                }
            }

            // 3. 큐에 다음 스케줄이 있다면, 다음 묶음/아이템 실행 전에 설정된 시간만큼 대기합니다.
            if (scheduleQueuePanel.transform.childCount > 0)
            {
                yield return new WaitForSeconds(delayBetweenSchedules);
            }
        }


        Debug.Log("모든 스케줄 실행 완료.");
        if (executeButton != null) executeButton.interactable = true;
        F_ProcessQueueCoroutine = null;
    }





    private void PerformScheduleBundle(ScheduleData dataToExecute, int bundleSizeN, NextScheduleModifiers modifiers)
    {
        Debug.Log($"--- '{dataToExecute.scheduleName}' (묶음 크기: {bundleSizeN}) 실행 시작 ---");

        float finalSuccessRate = dataToExecute.baseSuccessRate;
        // 기본 스탯 향상치는 '개별 카드'의 향상치 * 묶음 크기입니다.
        int baseStatImprovementForBundle = dataToExecute.primaryStatImprovementAmount * bundleSizeN;
        int finalStatImprovement = baseStatImprovementForBundle; // 연속 효과 보너스가 없다면 이 값이 최종 향상치가 됩니다.

        // 스트레스 변경치는 일단 기본값을 사용합니다.
        int finalStressChangeOnSuccess = dataToExecute.stressChangeOnSuccess;
        int finalStressChangeOnFailure = dataToExecute.stressChangeOnFailure;


        // 0. 연계 효과로 인한 보정 값 적용 (modifiers 객체에서 가져옴)
        if (modifiers != null)
        {
            
            if (modifiers.successRateBonus != 0)
            {

                finalSuccessRate += modifiers.successRateBonus;
                Debug.Log($"연계 효과로 성공 확률 +{modifiers.successRateBonus * 100:F0}% 적용됨.");
            }
            if (modifiers.statToBuff == dataToExecute.primaryTargetStat && modifiers.statBuffAmount != 0) // 주 대상 스탯과 일치할 때만 보너스
            {
                Debug.Log("추가 적용됨.................................");
                finalStatImprovement += modifiers.statBuffAmount;
                Debug.Log($"연계 효과로 {modifiers.statToBuff} 스탯 +{modifiers.statBuffAmount} 추가 적용됨.");
            }
            // TODO: 여기에 다른 modifier들(예: 스트레스 감소 보너스) 적용 로직 추가 가능
        }


        // 연속 효과 조건 확인 및 적용
        if (dataToExecute.canFormConsecutiveBundle && bundleSizeN >= dataToExecute.minItemsForConsecutiveEffect)
        {
            Debug.Log($"'{dataToExecute.scheduleName}' 연속 효과 발동! (묶음 크기: {bundleSizeN})");

            // 1. 성공 확률 보정
            if (dataToExecute.consecutiveSuccessRateModifierPerN != 0)
            {
                int nForPenaltyCalc = bundleSizeN;
                if (dataToExecute.maxNForSuccessRatePenaltyStack > 0 && dataToExecute.consecutiveSuccessRateModifierPerN < 0)
                {
                    nForPenaltyCalc = Mathf.Min(bundleSizeN, dataToExecute.maxNForSuccessRatePenaltyStack);
                }
                float successRateChange = dataToExecute.consecutiveSuccessRateModifierPerN * nForPenaltyCalc;
                finalSuccessRate += successRateChange;
                finalSuccessRate = Mathf.Clamp01(finalSuccessRate);
                Debug.Log($"연속 효과로 성공 확률 보정: {successRateChange * 100:F0}% -> 최종 성공 확률: {finalSuccessRate * 100:F0}%");
            }

            // 2. 스탯 보너스 적용 (applyStatBonusPerItemInBundle 값에 따라 분기)
            if (dataToExecute.consecutiveStatBonus != 0)
            {
                if (dataToExecute.applyStatBonusPerItemInBundle)
                {
                    int bonusToAdd = dataToExecute.consecutiveStatBonus * bundleSizeN;
                    finalStatImprovement += bonusToAdd;
                    Debug.Log($"연속 효과 추가 스탯 보너스 (아이템당 적용, 총): +{bonusToAdd}");
                }
                else
                {
                    finalStatImprovement += dataToExecute.consecutiveStatBonus;
                    Debug.Log($"연속 효과 추가 스탯 보너스 (묶음당 고정 적용): +{dataToExecute.consecutiveStatBonus}");
                }
            }
        }

        bool success = Random.value < finalSuccessRate;
        Debug.Log($"최종 성공 확률: {finalSuccessRate * 100:F0}% > 결과: {(success ? "성공!" : "실패...")}");

        string effectLog = "";

        if (success)
        {
            effectLog += "성공 효과: ";
            if (dataToExecute.primaryTargetStat != StatType.None && finalStatImprovement != 0)
            {
                // IdolCharacter의 기존 스탯 변경 함수들을 사용하여 스탯 적용
                switch (dataToExecute.primaryTargetStat)
                {
                    case StatType.Vocal:
                        idolCharacter.AddVocalPoint(finalStatImprovement);
                        break;
                    case StatType.Dance:
                        idolCharacter.AddDancePoint(finalStatImprovement);
                        break;
                    case StatType.Rap:
                        idolCharacter.AddRapPoint(finalStatImprovement);
                        break;
                    // 기획에 따라 다른 스탯 case 추가
                    default:
                        Debug.LogWarning($"PerformScheduleBundle: 처리되지 않은 primaryTargetStat - {dataToExecute.primaryTargetStat}에 대한 스탯 적용 로직이 없습니다.");
                        break;
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
}


