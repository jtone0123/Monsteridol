using UnityEngine;

[CreateAssetMenu(fileName = "IncreaseNextSuccess", menuName = "MyGame/Link Effects/Increase Next Success", order = 2)]
public class IncreaseNextSuccess : BaseOutgoingLinkEffect
{

    [Header("������ ������ ����")]
    [Tooltip("�ش� �������� �߰��� ������ ������")]
    [Range(0f, 1f)]
    public int additionalStatIncreaseAmount = 1;

    public override void ApplyToNext(IdolCharacter targetIdol, ScheduleData originScheduleData, ScheduleData targetNextScheduleData, NextScheduleModifiers nextScheduleTemporaryModifiers)
    {
        if(nextScheduleTemporaryModifiers != null)
        {
            nextScheduleTemporaryModifiers.successRateBonus += additionalStatIncreaseAmount; // ���ʽ� �� ���� ����
        }
        else
        {
            Debug.LogWarning($"IncreaseNextStatEffect: nextScheduleTemporaryModifiers ��ü�� null�Դϴ�. ȿ���� ������ �� �����ϴ�.");
        }
    }

    public override string GetDescription()
    {
        return $"���� ������ ���� �� �������� {additionalStatIncreaseAmount*100}%��ŭ �߰��� �����մϴ�. ({effectDescription})";
    }
}
