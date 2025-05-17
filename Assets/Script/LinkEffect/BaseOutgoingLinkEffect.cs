using UnityEngine;

/// <summary>
/// 모든 '발신형' 연계 효과 ScriptableObject의 기반이 되는 추상 클래스입니다.
/// 이 스케줄(Origin)이 실행된 후, 다음 스케줄(TargetNext)에 특정 효
/// 실제 다음 스케줄 실행 로직(예: PerformScheduleBundle)에서 이 보정 값들을 참조과를 적용합니다.
/// </summary>
public abstract class BaseOutgoingLinkEffect : ScriptableObject
{
    [Header("효과 설명 (UI 표시용)")]
    [TextArea(2, 4)]
    public string effectDescription = "이 효과에 대한 설명입니다.";

    /// <summary>
    /// 이 연계 효과를 다음 스케줄에 적용합니다.
    /// 이 메소드는 TempScheduleExecutor 같은 곳에서, 현재 스케줄(originScheduleData) 실행 후,
    /// 다음 스케줄(targetNextScheduleData)을 실행하기 직전에 호출될 수 있습니다.
    /// 다음 스케줄의 임시 속성(예: 임시 성공률, 임시 스탯 보너스)을 변경하는 방식으로 구현할 수 있습니다.
    /// </summary>
    /// <param name="targetIdol">효과를 받을 아이돌 캐릭터입니다.</param>
    /// <param name="originScheduleData">이 연계 효과를 발생시키는 현재 스케줄의 데이터입니다.</param>
    /// <param name="targetNextScheduleData">'다음으로' 실행될 예정이며, 이 효과가 적용될 대상 스케줄의 데이터입니다.</param>
    /// <param name="nextScheduleTemporaryModifiers">다음 스케줄 실행 시 적용될 임시 보정 값들을 저장하고 전달하는 객체입니다. (예: 성공률 보너스, 스탯 보너스 등)</param>
    public abstract void ApplyToNext(IdolCharacter targetIdol, ScheduleData originScheduleData, ScheduleData targetNextScheduleData, NextScheduleModifiers nextScheduleTemporaryModifiers);

    /// <summary>
    /// UI 등에 표시될 이 효과에 대한 설명을 반환합니다.
    /// 필요에 따라 동적으로 설명을 생성할 수 있습니다.
    /// </summary>
    public virtual string GetDescription()
    {
        return effectDescription;
    }
}

/// <summary>
/// 다음 스케줄 실행 시 적용될 임시적인 보정 값들을 담는 클래스입니다.
/// BaseOutgoingLinkEffect의 ApplyToNext 메소드에서 이 객체의 값을 수정하여,하여 사용합니다.
/// </summary>
[System.Serializable] // 필요에 따라, 하지만 보통은 실행 시점에 동적으로 생성하여 전달
public class NextScheduleModifiers
{
    public float successRateBonus = 0f; // 다음에 올 스케줄의 성공률에 추가될 보너스 (예: 0.1f는 +10%)
    public StatType statToBuff = StatType.None; // 다음에 올 스케줄의 결과로 특정 스탯에 추가 보너스를 줄 경우
    public int statBuffAmount = 0; // 해당 스탯에 추가될 보너스 양
    // 필요에 따라 다른 종류의 보정 값들 추가 가능 (예: 스트레스 감소 보너스, 비용 감소 등)

    public void Reset()
    {
        successRateBonus = 0f;
        statToBuff = StatType.None;
        statBuffAmount = 0;
    }
}
