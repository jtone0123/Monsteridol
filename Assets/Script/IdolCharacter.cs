// IdolCharacter.cs
using UnityEngine;
using System.Collections.Generic; // Dictionary 사용 시

// ScheduleData에서 사용할 StatType과 동일해야 함
public enum StatType { None, Vocal, Dance, Rap, Visual, Stamina } // 예시 스탯 타입

public class IdolCharacter : MonoBehaviour
{
    public string characterName = "idol";

    // 스탯 (Dictionary 또는 개별 변수로 관리)
    public Dictionary<StatType, int> stats = new Dictionary<StatType, int>();
    public int currentStress = 0;
    public int maxStress = 100; // 최대 스트레스 (예시)

    void Start()
    {
        // 초기 스탯 설정
        stats[StatType.Vocal] = 10;
        stats[StatType.Dance] = 10;
        stats[StatType.Rap] = 5;
        stats[StatType.Visual] = 20;
        stats[StatType.Stamina] = 50; // 스케줄 성공률 등에 영향 줄 수 있는 스탯 (나중에 활용)
        currentStress = 0;

        Debug.Log($"{characterName} 초기 상태: {GetCurrentStatus()}");
    }

    // 개별 스탯 변경 함수 (예시)
    public void AddVocalPoint(int amount)
    {
        if (stats.ContainsKey(StatType.Vocal))
        {
            stats[StatType.Vocal] += amount;
            Debug.Log($"{characterName} 보컬 능력치 {amount} 증가. 현재: {stats[StatType.Vocal]}");
        }
    }
    public void AddDancePoint(int amount)
    {
        if (stats.ContainsKey(StatType.Dance))
        {
            stats[StatType.Dance] += amount;
            Debug.Log($"{characterName} 댄스 능력치 {amount} 증가. 현재: {stats[StatType.Dance]}");
        }
    }

    public void AddRapPoint(int amount)
    {
        if (stats.ContainsKey(StatType.Rap))
        {
            stats[StatType.Rap] += amount;
            Debug.Log($"{characterName} 랩 능력치 {amount} 증가. 현재: {stats[StatType.Rap]}");
        }
    }
    // ... 다른 스탯 함수들도 유사하게 ...

    public void ChangeStress(int amount)
    {
        currentStress += amount;
        currentStress = Mathf.Clamp(currentStress, 0, maxStress); // 스트레스는 0과 maxStress 사이 값으로 제한
        Debug.Log($"{characterName} 스트레스 {amount} 변경. 현재: {currentStress}/{maxStress}");
        // TODO: 스트레스 수치에 따른 디버프 또는 부정적 특성 발현 로직 (나중에 추가)
    }

    public string GetCurrentStatus()
    {
        string status = $"스트레스: {currentStress}/{maxStress}";
        foreach (var statEntry in stats)
        {
            status += $", {statEntry.Key}: {statEntry.Value}";
        }
        return status;
    }
}