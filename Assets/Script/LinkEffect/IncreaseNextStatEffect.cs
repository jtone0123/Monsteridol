using UnityEngine;

[CreateAssetMenu(fileName = "NewIncreaseNextStatEffect", menuName = "MyGame/Link Effects/Increase Next Stat", order = 2)]
public class IncreaseNextStatEffect : BaseOutgoingLinkEffect
{
    [Header("스탯 증가량 설정")]
    [Tooltip("다음 스케줄의 결과로 증가될 스탯 타입")]
    public StatType statToIncrease = StatType.None;

    [Tooltip("해당 스탯에 추가로 더해질 증가량")]
    public int additionalStatIncreaseAmount = 1;

    public override void ApplyToNext(IdolCharacter targetIdol, ScheduleData originScheduleData, ScheduleData targetNextScheduleData, NextScheduleModifiers nextScheduleTemporaryModifiers)
    {
        if (nextScheduleTemporaryModifiers != null)
        {
            // 이 효과는 다음 스케줄의 '주 대상 스탯(primaryTargetStat)'과 이 효과의 'statToIncrease'가 일치할 때만
            // 보너스를 주는 방식으로 할 수도 있고, 또는 무조건 statToIncrease에 보너스를 주도록 할 수도 있습니다.
            // 여기서는 다음 스케줄의 결과에 이 스탯 보너스가 '추가'되는 개념으로 가정합니다.
            // 실제 스탯 적용은 다음 스케줄의 Perform 로직에서 NextScheduleModifiers를 참조하여 이루어집니다.

            nextScheduleTemporaryModifiers.statToBuff = statToIncrease; // 어떤 스탯에 보너스를 줄지 기록
            nextScheduleTemporaryModifiers.statBuffAmount += additionalStatIncreaseAmount; // 보너스 양 누적 가능

            Debug.Log($"연계 효과 발동: '{originScheduleData.scheduleName}'의 영향으로 다음 스케줄 '{targetNextScheduleData.scheduleName}' 실행 시 {statToIncrease} 스탯에 +{additionalStatIncreaseAmount} 추가 보너스가 적용될 예정입니다.");
        }
        else
        {
            Debug.LogWarning($"IncreaseNextStatEffect: nextScheduleTemporaryModifiers 객체가 null입니다. 효과를 적용할 수 없습니다.");
        }
    }

    public override string GetDescription()
    {
        return $"다음 스케줄 실행 시 {statToIncrease} 스탯 획득량이 {additionalStatIncreaseAmount}만큼 추가로 증가합니다. ({effectDescription})";
    }
}
