using System.Collections.Generic;
using UnityEngine;

// IdolCharacter.cs 에도 동일한 StatType enum이 있어야 합니다.
// public enum StatType { None, Vocal, Dance, Rap, Visual, Stamina } // 이 부분을 IdolCharacter.cs 와 공유하거나 한 곳에서 정의

[CreateAssetMenu(fileName = "NewScheduleData", menuName = "MyGame/Schedule Data", order = 1)]
public class ScheduleData : ScriptableObject
{
        

    [Header("기본 정보")]
    public string scheduleName = "새 스케줄";
    [TextArea(3, 5)]
    public string description = "스케줄에 대한 설명입니다.";
    public Sprite icon;
    public int cost = 1; // 스케줄 실행 비용 (예: 행동력)

    [Header("실행 효과")]
    [Range(0f, 1f)] // 0.0 (0%) ~ 1.0 (100%)
    public float baseSuccessRate = 0.7f; // 기본 성공 확률

    // 이 스케줄이 성공했을 때 주로 어떤 능력치를 얼마나 올릴 것인가
    public StatType primaryTargetStat = StatType.None; // 주로 영향을 주는 능력치 타입
    public int primaryStatImprovementAmount = 5;   // 해당 능력치 향상 정도 (성공 시)

    public int stressChangeOnSuccess = 2;      // 성공 시 스트레스 변화량

    // 실패했을 때의 효과
    // public StatType statPenaltyTargetStat = StatType.None; // 실패 시 영향을 줄 스탯 (필요하다면)
    // public int statPenaltyAmount = 0;       // 실패 시 스탯 감소량 (0 또는 음수)
    public int stressChangeOnFailure = 10;     // 실패 시 스트레스 변화량

    [Header("연속 효과 (2단계)")]
    [Tooltip("이 스케줄이 연속 묶음을 형성하여 특별한 효과를 가질 수 있는지 여부")]
    public bool canFormConsecutiveBundle = false;

    [Tooltip("연속 효과가 발동하기 위한 최소 묶음 아이템 개수 (예: 2개 이상 연속 시)")]
    public int minItemsForConsecutiveEffect = 2;

    [Tooltip("묶음 내 아이템 개수(n)당 기본 성공 확률에 더해지거나 빼지는 값. 예: -0.2는 n개당 20%씩 감소")]
    public float consecutiveSuccessRateModifierPerN = 0f;

    [Tooltip("연속 효과로 인해 주 대상 스탯(primaryTargetStat)에 추가되는 보너스 수치")]
    public int consecutiveStatBonus = 0;

    [Tooltip("consecutiveStatBonus가 묶음 내 각 아이템마다 적용될지 (true), 아니면 묶음 전체에 한 번만 적용될지 (false) 결정")]
    public bool applyStatBonusPerItemInBundle = false;

    [Tooltip("성공 확률 페널티(consecutiveSuccessRateModifierPerN < 0 인 경우)가 누적되는 최대 n값. 0이면 제한 없음.")]
    public int maxNForSuccessRatePenaltyStack = 0;



    // 연계 효과 하나를 정의하는 내부 클래스 (또는 구조체)
    [Header("연계 효과 (3단계 - 발신형)")]
    [Tooltip("이 스케줄이 실행된 후, 특정 조건의 다음 스케줄에 적용할 수 있는 효과 목록")]
    public List<OutgoingLinkEffectRule> outgoingLinkEffectRules;

    // 이 스케줄(A)이 다음에 오는 스케줄(B)에 어떤 영향을 줄지 정의하는 내부 클래스
    [System.Serializable]
    public class OutgoingLinkEffectRule
    {
        [Tooltip("이 효과가 적용될 '다음' 스케줄의 조건 (예: 특정 스케줄 데이터와 일치해야 함)")]
        public ScheduleData targetNextScheduleCondition; // 다음에 이 스케줄이 와야 효과 발동

        [Tooltip("위 조건이 만족되면 '다음' 스케줄에 적용될 특수 연계 효과 에셋")]
        public BaseOutgoingLinkEffect effectToApplyOnNextSchedule; // 어떤 효과를 다음 스케줄에 줄 것인가 (BaseOutgoingLinkEffect는 다음 단계에서 정의)
    }
}