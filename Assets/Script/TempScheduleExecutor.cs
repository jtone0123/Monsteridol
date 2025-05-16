using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class TempScheduleExecutor : MonoBehaviour
{
    [Header("���� �ʼ� (Inspector���� �Ҵ�)")]
    public ScheduleDropZone scheduleQueuePanel;
    public IdolCharacter idolCharacter;

    [Header("���� ����")]
    public float delayBetweenSchedules = 1.5f;

    [Header("���� ���� (��ư UI ����� ���)")]
    public Button executeButton;

    private PlayerInputActions playerInputActions;
    private Coroutine F_ProcessQueueCoroutine = null;

    void Awake()
    {
        playerInputActions = new PlayerInputActions();
        if (scheduleQueuePanel == null) Debug.LogError("TempScheduleExecutor: ScheduleQueuePanel�� Inspector�� �Ҵ���� �ʾҽ��ϴ�!");
        if (idolCharacter == null) Debug.LogError("TempScheduleExecutor: IdolCharacter�� Inspector�� �Ҵ���� �ʾҽ��ϴ�!");
    }

    void OnEnable()
    {
        // Input Actions ������ ���� Action Map �̸��� ��� (��: "Gameplay")
        if (playerInputActions.ExecuteSchedule.Get() != null)
        {
            playerInputActions.ExecuteSchedule.Enable();
            if (playerInputActions.ExecuteSchedule.start != null)
            {
                playerInputActions.ExecuteSchedule.start.performed += OnExecuteScheduleInput;
            }
            else Debug.LogError("TempScheduleExecutor: 'ExecuteSchedule' Action�� 'Gameplay' Action Map���� ã�� �� �����ϴ�.");
        }
        else Debug.LogError("TempScheduleExecutor: 'Gameplay' Action Map�� ã�� �� �����ϴ�. Input Actions ���� ������ Ȯ�����ּ���.");

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
        Debug.Log("������ ť ���� �Է� ����! (New Input System)");
        StartProcessingQueue();
    }

    public void StartProcessingQueue()
    {
        if (F_ProcessQueueCoroutine != null)
        {
            Debug.LogWarning("�̹� ������ ť�� ó�� ���Դϴ�.");
            return;
        }
        if (scheduleQueuePanel == null || idolCharacter == null)
        {
            Debug.LogWarning("���࿡ �ʿ��� ���(�г� �Ǵ� ���̵�)�� �������� �ʾҽ��ϴ�.");
            return;
        }
        if (scheduleQueuePanel.transform.childCount == 0)
        {
            Debug.Log("������ �������� ť�� �����ϴ�.");
            return;
        }
        F_ProcessQueueCoroutine = StartCoroutine(ProcessScheduleQueueCoroutine());
    }

    IEnumerator ProcessScheduleQueueCoroutine()
    {
        Debug.Log("������ ť �ڵ� ���� ����...");
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
                Debug.LogWarning("ť�� ù ��° �����ۿ��� DraggableScheduleItem �Ǵ� ScheduleData�� ã�� �� �����ϴ�. �������� �����մϴ�.");
                if (firstItemTransform != null) Destroy(firstItemTransform.gameObject);
            }
        }

        Debug.Log("��� ������ ���� �Ϸ�.");
        if (executeButton != null) executeButton.interactable = true;
        F_ProcessQueueCoroutine = null;
    }

    private void PerformSingleScheduleAndLog(ScheduleData dataToExecute)
    {
        Debug.Log($"--- '{dataToExecute.scheduleName}' ������ ���� ���� ---");

        bool success = Random.value < dataToExecute.baseSuccessRate;
        Debug.Log($"���� Ȯ��: {dataToExecute.baseSuccessRate * 100:F0}% > ���: {(success ? "����!" : "����...")}");

        string effectLog = "";

        if (success)
        {
            effectLog += "���� ȿ��: ";
            if (dataToExecute.primaryTargetStat != StatType.None)
            {
                // ���� ���� �α׿� ������� IdolCharacter�� getter �Լ����� �ʿ��մϴ�.
                // ���⼭�� ������ ���淮�� ǥ���ϰų�, GetCurrentStatus()�� �����մϴ�.
                switch (dataToExecute.primaryTargetStat)
                {
                    case StatType.Vocal:
                        // int oldVal = idolCharacter.GetCurrentVocalPoint(); // getter�� �ִٸ�
                        idolCharacter.AddVocalPoint(dataToExecute.primaryStatImprovementAmount);
                        effectLog += $"���� +{dataToExecute.primaryStatImprovementAmount}, ";
                        break;
                    case StatType.Dance:
                        idolCharacter.AddDancePoint(dataToExecute.primaryStatImprovementAmount);
                        effectLog += $"�� +{dataToExecute.primaryStatImprovementAmount}, ";
                        break;
                    case StatType.Rap:
                        idolCharacter.AddRapPoint(dataToExecute.primaryStatImprovementAmount);
                        effectLog += $"�� +{dataToExecute.primaryStatImprovementAmount}, ";
                        break;
                    // case StatType.Charm: // ���� Charm ������ �ִٸ�
                    //     idolCharacter.AddCharmPoint(dataToExecute.primaryStatImprovementAmount);
                    //     effectLog += $"�ŷ� +{dataToExecute.primaryStatImprovementAmount}, ";
                    //     break;
                    default:
                        Debug.LogWarning($"PerformSingleScheduleAndLog: ó������ ���� primaryTargetStat - {dataToExecute.primaryTargetStat}");
                        break;
                }
            }
            idolCharacter.ChangeStress(dataToExecute.stressChangeOnSuccess);
            effectLog += $"��Ʈ���� {(dataToExecute.stressChangeOnSuccess >= 0 ? "+" : "") + dataToExecute.stressChangeOnSuccess}";
        }
        else
        {
            effectLog += "���� ȿ��: ";
            idolCharacter.ChangeStress(dataToExecute.stressChangeOnFailure);
            effectLog += $"��Ʈ���� {(dataToExecute.stressChangeOnFailure >= 0 ? "+" : "") + dataToExecute.stressChangeOnFailure}";
        }

        Debug.Log(effectLog.TrimEnd(' ', ','));
        Debug.Log($"'{dataToExecute.scheduleName}' ������ ���� �Ϸ�. ���� ���̵� ����: {idolCharacter.GetCurrentStatus()}");
        Debug.Log("------------------------------------");
    }
}