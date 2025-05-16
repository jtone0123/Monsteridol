using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

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

    void Awake()
    {
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
            DraggableScheduleItem scheduleItem = firstItemTransform.GetComponent<DraggableScheduleItem>();

            if (scheduleItem != null && scheduleItem.scheduleData != null)
            {
                PerformSingleScheduleAndLog(scheduleItem.scheduleData);
                Destroy(firstItemTransform.gameObject);
                if (scheduleQueuePanel.transform.childCount > 0)
                {
                    yield return new WaitForSeconds(delayBetweenSchedules);
                }
            }
            else
            {
                Debug.LogWarning("큐의 첫 번째 아이템에서 DraggableScheduleItem 또는 ScheduleData를 찾을 수 없습니다. 아이템을 제거합니다.");
                if (firstItemTransform != null) Destroy(firstItemTransform.gameObject);
            }
        }

        Debug.Log("모든 스케줄 실행 완료.");
        if (executeButton != null) executeButton.interactable = true;
        F_ProcessQueueCoroutine = null;
    }

    private void PerformSingleScheduleAndLog(ScheduleData dataToExecute)
    {
        Debug.Log($"--- '{dataToExecute.scheduleName}' 스케줄 실행 시작 ---");

        bool success = Random.value < dataToExecute.baseSuccessRate;
        Debug.Log($"성공 확률: {dataToExecute.baseSuccessRate * 100:F0}% > 결과: {(success ? "성공!" : "실패...")}");

        string effectLog = "";

        if (success)
        {
            effectLog += "성공 효과: ";
            if (dataToExecute.primaryTargetStat != StatType.None)
            {
                // 이전 값을 로그에 남기려면 IdolCharacter에 getter 함수들이 필요합니다.
                // 여기서는 간단히 변경량만 표시하거나, GetCurrentStatus()에 의존합니다.
                switch (dataToExecute.primaryTargetStat)
                {
                    case StatType.Vocal:
                        // int oldVal = idolCharacter.GetCurrentVocalPoint(); // getter가 있다면
                        idolCharacter.AddVocalPoint(dataToExecute.primaryStatImprovementAmount);
                        effectLog += $"보컬 +{dataToExecute.primaryStatImprovementAmount}, ";
                        break;
                    case StatType.Dance:
                        idolCharacter.AddDancePoint(dataToExecute.primaryStatImprovementAmount);
                        effectLog += $"댄스 +{dataToExecute.primaryStatImprovementAmount}, ";
                        break;
                    case StatType.Rap:
                        idolCharacter.AddRapPoint(dataToExecute.primaryStatImprovementAmount);
                        effectLog += $"랩 +{dataToExecute.primaryStatImprovementAmount}, ";
                        break;
                    // case StatType.Charm: // 만약 Charm 스탯이 있다면
                    //     idolCharacter.AddCharmPoint(dataToExecute.primaryStatImprovementAmount);
                    //     effectLog += $"매력 +{dataToExecute.primaryStatImprovementAmount}, ";
                    //     break;
                    default:
                        Debug.LogWarning($"PerformSingleScheduleAndLog: 처리되지 않은 primaryTargetStat - {dataToExecute.primaryTargetStat}");
                        break;
                }
            }
            idolCharacter.ChangeStress(dataToExecute.stressChangeOnSuccess);
            effectLog += $"스트레스 {(dataToExecute.stressChangeOnSuccess >= 0 ? "+" : "") + dataToExecute.stressChangeOnSuccess}";
        }
        else
        {
            effectLog += "실패 효과: ";
            idolCharacter.ChangeStress(dataToExecute.stressChangeOnFailure);
            effectLog += $"스트레스 {(dataToExecute.stressChangeOnFailure >= 0 ? "+" : "") + dataToExecute.stressChangeOnFailure}";
        }

        Debug.Log(effectLog.TrimEnd(' ', ','));
        Debug.Log($"'{dataToExecute.scheduleName}' 스케줄 실행 완료. 현재 아이돌 상태: {idolCharacter.GetCurrentStatus()}");
        Debug.Log("------------------------------------");
    }
}