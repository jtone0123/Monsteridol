using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

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

    // ������ ����� �����ٿ� ����� �ӽ� ���� ������ �����ϴ� ��ü
    private NextScheduleModifiers pendingModifiersForNextSchedule;
    void Awake()
    {
        pendingModifiersForNextSchedule = new NextScheduleModifiers();
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
            DraggableScheduleItem firstScheduleItemInQueue = firstItemTransform.GetComponent<DraggableScheduleItem>();

            if (firstScheduleItemInQueue == null || firstScheduleItemInQueue.scheduleData == null)
            {
                Debug.LogWarning("ť�� ù ��° �������� ��ȿ���� �ʽ��ϴ�. �����մϴ�.");
                if (scheduleQueuePanel.transform.childCount > 0) Destroy(scheduleQueuePanel.transform.GetChild(0).gameObject);
                continue; // ���� ������
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
                    // ���� �����۰� �ٸ� ScheduleData�� ���� �������� �߰ߵǸ� ���� ����
                    break;
                }
            }


            // 2. �ĺ��� ������ �����մϴ�.
            if (bundleSizeN > 0)
            {
                Debug.Log($"'{currentScheduleData.scheduleName}' ������ ���� (ũ��: {bundleSizeN}) ó�� ����.");
                PerformScheduleBundle(currentScheduleData, bundleSizeN, pendingModifiersForNextSchedule);
                pendingModifiersForNextSchedule.Reset(); // ���� �������� ���� ������̾� �ʱ�ȭ

                // ����� ������ ���Ե� ��� UI �����۵��� ť���� �����մϴ�.
                foreach (Transform itemToRemove in itemsInBundle)
                {
                    
                    Destroy(itemToRemove.gameObject);
                }
            }
            yield return null;


            // --- ���� ȿ�� �ߵ� �غ� ---
            if (scheduleQueuePanel.transform.childCount > 0) // ������ ������ �������� �ִٸ�
            {
                DraggableScheduleItem nextScheduleItem = scheduleQueuePanel.transform.GetChild(0).GetComponent<DraggableScheduleItem>();
                if (nextScheduleItem != null && nextScheduleItem.scheduleData != null)
                {
                    ScheduleData nextScheduleInQueueData = nextScheduleItem.scheduleData;
                    // currentScheduleData�� �߽� ���� ȿ�� ��Ģ���� Ȯ��
                    if (currentScheduleData.outgoingLinkEffectRules != null)
                    {
                        foreach (var rule in currentScheduleData.outgoingLinkEffectRules)
                        {
                            // ���� �������� ��Ģ�� ���ǰ� ��ġ�ϰ�, ������ ȿ���� �ִٸ�
                            
                           
                             if (rule.targetNextScheduleCondition == nextScheduleInQueueData && rule.effectToApplyOnNextSchedule != null)
                            {
                                Debug.Log($"'{currentScheduleData.scheduleName}'�� ���� ������ '{nextScheduleInQueueData.scheduleName}'�� ���� ȿ���� �غ��մϴ�: {rule.effectToApplyOnNextSchedule.effectDescription}");
                                // pendingModifiersForNextSchedule�� �̹� ������ Reset() �Ǿ����Ƿ�, ���⿡ �� ȿ���� ����
                                rule.effectToApplyOnNextSchedule.ApplyToNext(idolCharacter, currentScheduleData, nextScheduleInQueueData, pendingModifiersForNextSchedule);
                            }
                        }
                        
                    }
                    
                }
            }

            // 3. ť�� ���� �������� �ִٸ�, ���� ����/������ ���� ���� ������ �ð���ŭ ����մϴ�.
            if (scheduleQueuePanel.transform.childCount > 0)
            {
                yield return new WaitForSeconds(delayBetweenSchedules);
            }
        }


        Debug.Log("��� ������ ���� �Ϸ�.");
        if (executeButton != null) executeButton.interactable = true;
        F_ProcessQueueCoroutine = null;
    }





    private void PerformScheduleBundle(ScheduleData dataToExecute, int bundleSizeN, NextScheduleModifiers modifiers)
    {
        Debug.Log($"--- '{dataToExecute.scheduleName}' (���� ũ��: {bundleSizeN}) ���� ���� ---");

        float finalSuccessRate = dataToExecute.baseSuccessRate;
        // �⺻ ���� ���ġ�� '���� ī��'�� ���ġ * ���� ũ���Դϴ�.
        int baseStatImprovementForBundle = dataToExecute.primaryStatImprovementAmount * bundleSizeN;
        int finalStatImprovement = baseStatImprovementForBundle; // ���� ȿ�� ���ʽ��� ���ٸ� �� ���� ���� ���ġ�� �˴ϴ�.

        // ��Ʈ���� ����ġ�� �ϴ� �⺻���� ����մϴ�.
        int finalStressChangeOnSuccess = dataToExecute.stressChangeOnSuccess;
        int finalStressChangeOnFailure = dataToExecute.stressChangeOnFailure;


        // 0. ���� ȿ���� ���� ���� �� ���� (modifiers ��ü���� ������)
        if (modifiers != null)
        {
            
            if (modifiers.successRateBonus != 0)
            {

                finalSuccessRate += modifiers.successRateBonus;
                Debug.Log($"���� ȿ���� ���� Ȯ�� +{modifiers.successRateBonus * 100:F0}% �����.");
            }
            if (modifiers.statToBuff == dataToExecute.primaryTargetStat && modifiers.statBuffAmount != 0) // �� ��� ���Ȱ� ��ġ�� ���� ���ʽ�
            {
                Debug.Log("�߰� �����.................................");
                finalStatImprovement += modifiers.statBuffAmount;
                Debug.Log($"���� ȿ���� {modifiers.statToBuff} ���� +{modifiers.statBuffAmount} �߰� �����.");
            }
            // TODO: ���⿡ �ٸ� modifier��(��: ��Ʈ���� ���� ���ʽ�) ���� ���� �߰� ����
        }


        // ���� ȿ�� ���� Ȯ�� �� ����
        if (dataToExecute.canFormConsecutiveBundle && bundleSizeN >= dataToExecute.minItemsForConsecutiveEffect)
        {
            Debug.Log($"'{dataToExecute.scheduleName}' ���� ȿ�� �ߵ�! (���� ũ��: {bundleSizeN})");

            // 1. ���� Ȯ�� ����
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
                Debug.Log($"���� ȿ���� ���� Ȯ�� ����: {successRateChange * 100:F0}% -> ���� ���� Ȯ��: {finalSuccessRate * 100:F0}%");
            }

            // 2. ���� ���ʽ� ���� (applyStatBonusPerItemInBundle ���� ���� �б�)
            if (dataToExecute.consecutiveStatBonus != 0)
            {
                if (dataToExecute.applyStatBonusPerItemInBundle)
                {
                    int bonusToAdd = dataToExecute.consecutiveStatBonus * bundleSizeN;
                    finalStatImprovement += bonusToAdd;
                    Debug.Log($"���� ȿ�� �߰� ���� ���ʽ� (�����۴� ����, ��): +{bonusToAdd}");
                }
                else
                {
                    finalStatImprovement += dataToExecute.consecutiveStatBonus;
                    Debug.Log($"���� ȿ�� �߰� ���� ���ʽ� (������ ���� ����): +{dataToExecute.consecutiveStatBonus}");
                }
            }
        }

        bool success = Random.value < finalSuccessRate;
        Debug.Log($"���� ���� Ȯ��: {finalSuccessRate * 100:F0}% > ���: {(success ? "����!" : "����...")}");

        string effectLog = "";

        if (success)
        {
            effectLog += "���� ȿ��: ";
            if (dataToExecute.primaryTargetStat != StatType.None && finalStatImprovement != 0)
            {
                // IdolCharacter�� ���� ���� ���� �Լ����� ����Ͽ� ���� ����
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
                    // ��ȹ�� ���� �ٸ� ���� case �߰�
                    default:
                        Debug.LogWarning($"PerformScheduleBundle: ó������ ���� primaryTargetStat - {dataToExecute.primaryTargetStat}�� ���� ���� ���� ������ �����ϴ�.");
                        break;
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
}


