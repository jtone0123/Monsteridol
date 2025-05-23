using UnityEngine;

[CreateAssetMenu(fileName = "IncreaseNextSuccess", menuName = "MyGame/Link Effects/Increase Next Success", order = 2)]
public class IncreaseNextSuccess : BaseOutgoingLinkEffect
{

    [Header("성공률 증가량 설정")]
    [Tooltip("해당 성공률에 추가로 더해질 증가량")]
    [Range(0.0f,10.0f)]
    public float additionalStatIncreaseAmount = 1; // 원래 확률에 10곱한 값

    public override void ApplyToNext(IdolCharacter targetIdol, ScheduleData originScheduleData, ScheduleData targetNextScheduleData, NextScheduleModifiers nextScheduleTemporaryModifiers)
    {
        if(nextScheduleTemporaryModifiers != null)
        {
            nextScheduleTemporaryModifiers.successRateBonus += additionalStatIncreaseAmount; // 보너스 양 누적 가능
        }
        else
        {
            Debug.LogWarning($"IncreaseNextStatEffect: nextScheduleTemporaryModifiers 객체가 null입니다. 효과를 적용할 수 없습니다.");
        }
    }

    public override string GetDescription()
    {
        return $"다음 스케줄 실행 시 성공률이 {additionalStatIncreaseAmount*10}%만큼 추가로 증가합니다. ({effectDescription})";
    }
}
