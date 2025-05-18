using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class TempScheduleExecutor : MonoBehaviour
{
    [Header("���� �ʼ� (Inspector���� �Ҵ�)")]
    public ScheduleDropZone scheduleQueuePanel; // ������ �����۵��� ��ġ�Ǵ� �г�
    public IdolCharacter idolCharacter;      // ������ ȿ���� ���� ���̵�
    public UIManager uiManager; 



    [Header("���� ����")]
    [Tooltip("�� ������ '����' �Ǵ� ���� ������ ���� ��, ���� ������ ��������� ��� �ð� (��)")]
    public float delayBetweenSchedules = 1.5f;
    [Tooltip("������ ���� �� ������ �������� �ö���� �ִϸ��̼ǰ� ���� ������ ���� ������ �߰� ������ (���� ����)")]
    public float delayAfterLayoutAnimation = 0.3f; // RefreshLayout �ִϸ��̼� �ð� ���

    [Header("���� ���� (��ư UI ����� ���)")]
    public Button executeButton;

    private PlayerInputActions playerInputActions;
    private Coroutine F_ProcessQueueCoroutine = null;
    private NextScheduleModifiers pendingModifiersForNextSchedule;

    void Awake()
    {
        playerInputActions = new PlayerInputActions();
        pendingModifiersForNextSchedule = new NextScheduleModifiers();

        if (scheduleQueuePanel == null) Debug.LogError("TempScheduleExecutor: ScheduleQueuePanel�� Inspector�� �Ҵ���� �ʾҽ��ϴ�!");
        if (idolCharacter == null) Debug.LogError("TempScheduleExecutor: IdolCharacter�� Inspector�� �Ҵ���� �ʾҽ��ϴ�!");
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
            else Debug.LogError("TempScheduleExecutor: 'ExecuteSchedule' Action�� 'Gameplay' Action Map���� ã�� �� �����ϴ�.");
        }
        else Debug.LogError("TempScheduleExecutor: 'Gameplay' Action Map�� ã�� �� �����ϴ�.");

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
        Debug.Log("������ ť ���� �Է� ����!");
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
            DraggableScheduleItem firstScheduleItemInQueue = scheduleQueuePanel.transform.GetChild(0).GetComponent<DraggableScheduleItem>();
            if (firstScheduleItemInQueue == null || firstScheduleItemInQueue.scheduleData == null)
            {
                Debug.LogWarning("ť�� ù ��° �������� ��ȿ���� �ʽ��ϴ�. �����մϴ�.");
                if (scheduleQueuePanel.transform.childCount > 0) Destroy(scheduleQueuePanel.transform.GetChild(0).gameObject);
                yield return null; // Destroy �ݿ� ���
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
                Debug.Log($"'{currentScheduleData.scheduleName}' ������ ���� (ũ��: {bundleSizeN}) ó�� ����.");
                PerformScheduleBundle(currentScheduleData, bundleSizeN, pendingModifiersForNextSchedule);
                pendingModifiersForNextSchedule.Reset();

                foreach (Transform itemToRemove in itemsInBundle)
                {
                    if (itemToRemove != null) Destroy(itemToRemove.gameObject);
                }

                // �߿�: Destroy ȣ�� ��, ���� �ı��� �ݿ��� ������ �� ������ ����մϴ�.
                yield return null;
                // �Ǵ� yield return new WaitForEndOfFrame(); // �������� ������ ���

                // ������ ���� ��, �����ִ� �����۵��� ���̾ƿ��� �ִϸ��̼����� �����մϴ�.
                if (scheduleQueuePanel.transform.childCount > 0) // �����ִ� �������� ���� ����
                {
                    Debug.Log("�����ִ� �����ٵ� ���� �ø��� �ִϸ��̼� ����.");
                    scheduleQueuePanel.RefreshLayout(true,true); // animate: true //UseTweem = true 
                    // RefreshLayout ������ �ִϸ��̼� �ð�(siblingAnimationDuration)��ŭ ��ٷ��ִ� ���� ����
                    // �Ǵ�, RefreshLayout�� �ڷ�ƾ�� �ƴ϶��, ���⼭ �� �ð���ŭ ���
                    yield return new WaitForSeconds(scheduleQueuePanel.siblingAnimationDuration + delayAfterLayoutAnimation);
                }


                // ���� �����ٿ� ���� ���� ȿ�� �غ�
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

            // ���� ����/������ ���� �� ��� (�� ���� ���� ȿ�� �غ� ��, ���� ���� ���� ��)
            if (scheduleQueuePanel.transform.childCount > 0)
            {
                // delayBetweenSchedules�� �̹� RefreshLayout �ִϸ��̼� ������ ������ �ǹ��ϰ� ��
                // ���� RefreshLayout �ִϸ��̼� �ð��� ������ ������ �ְ� �ʹٸ�,
                // ���� WaitForSeconds(scheduleQueuePanel.siblingAnimationDuration) �� ���⼭�� delayBetweenSchedules�� ����.
                // ����� RefreshLayout �ִϸ��̼� �� �߰� delayAfterLayoutAnimation, �� �� ���� ���� ���� �� delayBetweenSchedules
                yield return new WaitForSeconds(delayBetweenSchedules);
            }
        }

        Debug.Log("��� ������ ���� �Ϸ�.");
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
        // ... (�� �Լ� ������ ������ �����ϰ� ����) ...
        Debug.Log($"--- '{dataToExecute.scheduleName}' (���� ũ��: {bundleSizeN}) ���� ���� ---");

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
                Debug.Log($"���� ȿ���� ���� Ȯ�� +{modifiers.successRateBonus * 100:F0}% �����.");
            }
            if (modifiers.statToBuff == dataToExecute.primaryTargetStat && modifiers.statBuffAmount != 0)
            {
                finalStatImprovement += modifiers.statBuffAmount;
                Debug.Log($"���� ȿ���� {modifiers.statToBuff} ���� +{modifiers.statBuffAmount} �߰� �����.");
            }
        }

        if (dataToExecute.canFormConsecutiveBundle && bundleSizeN >= dataToExecute.minItemsForConsecutiveEffect)
        {
            Debug.Log($"'{dataToExecute.scheduleName}' ���� ȿ�� �ߵ�! (���� ũ��: {bundleSizeN})");
            if (dataToExecute.consecutiveSuccessRateModifierPerN != 0)
            {
                int nForPenaltyCalc = bundleSizeN;
                if (dataToExecute.maxNForSuccessRatePenaltyStack > 0 && dataToExecute.consecutiveSuccessRateModifierPerN < 0)
                {
                    nForPenaltyCalc = Mathf.Min(bundleSizeN, dataToExecute.maxNForSuccessRatePenaltyStack);
                }
                float successRateChange = dataToExecute.consecutiveSuccessRateModifierPerN * nForPenaltyCalc;
                finalSuccessRate += successRateChange;
                Debug.Log($"���� ȿ���� ���� Ȯ�� ����: {successRateChange * 100:F0}%");
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
                Debug.Log($"���� ȿ�� ���� ���ʽ� �����.");
            }
        }
        finalSuccessRate = Mathf.Clamp01(finalSuccessRate);

        bool success = Random.value < finalSuccessRate;
        Debug.Log($"���� ���� Ȯ��: {finalSuccessRate * 100:F0}% > ���: {(success ? "����!" : "����...")}");
        string effectLog = "";
        if (success)
        {
            effectLog += "���� ȿ��: ";
            if (dataToExecute.primaryTargetStat != StatType.None && finalStatImprovement != 0)
            {
                switch (dataToExecute.primaryTargetStat)
                {
                    case StatType.Vocal: idolCharacter.AddVocalPoint(finalStatImprovement); break;
                    case StatType.Dance: idolCharacter.AddDancePoint(finalStatImprovement); break;
                    case StatType.Rap: idolCharacter.AddRapPoint(finalStatImprovement); break;
                    default: Debug.LogWarning($"PerformScheduleBundle: ó������ ���� primaryTargetStat - {dataToExecute.primaryTargetStat}�� ���� ���� ���� ������ �����ϴ�."); break;
                }
                effectLog += $"{dataToExecute.primaryTargetStat} +{finalStatImprovement}, ";
            }
            idolCharacter.ChangeStress(finalStressChangeOnSuccess);
            effectLog += $"��Ʈ���� {(finalStressChangeOnSuccess >= 0 ? "+" : "") + finalStressChangeOnSuccess}";
        }
        else
        {
            effectLog += "���� ȿ��: ";
            idolCharacter.ChangeStress(finalStressChangeOnFailure);
            effectLog += $"��Ʈ���� {(finalStressChangeOnFailure >= 0 ? "+" : "") + finalStressChangeOnFailure}";
        }
        Debug.Log(effectLog.TrimEnd(' ', ','));
        Debug.Log($"'{dataToExecute.scheduleName}' (���� ũ��: {bundleSizeN}) ���� �Ϸ�. ���� ���̵� ����: {idolCharacter.GetCurrentStatus()}");
        Debug.Log("------------------------------------");
    }
   
    public List<GameObject> scheduleItemPrefab;
    public Transform availableSchedulesPanel; // Inspector���� �Ҵ��� �θ� �г�

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
            Debug.LogError("������ ������ �Ǵ� �θ� �г��� �Ҵ���� �ʾҽ��ϴ�!");
            return;
        }
           // 1. �������� ����Ͽ� �� ������ ������ ���� ������Ʈ�� �����մϴ�.
        // Instantiate�� �� ��° �Ķ���ͷ� �θ� Transform�� �����ϸ� �ٷ� �ڽ����� �����˴ϴ�.
        GameObject newScheduleObject = Instantiate(scheduleItemPrefab[i], availableSchedulesPanel);
    }
}
