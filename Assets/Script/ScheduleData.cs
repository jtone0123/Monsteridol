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

    // TODO: 연속/연계 효과 필드는 다음 단계에서 추가
}