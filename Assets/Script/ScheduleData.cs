using UnityEngine;

// IdolCharacter.cs ���� ������ StatType enum�� �־�� �մϴ�.
// public enum StatType { None, Vocal, Dance, Rap, Visual, Stamina } // �� �κ��� IdolCharacter.cs �� �����ϰų� �� ������ ����

[CreateAssetMenu(fileName = "NewScheduleData", menuName = "MyGame/Schedule Data", order = 1)]
public class ScheduleData : ScriptableObject
{
    [Header("�⺻ ����")]
    public string scheduleName = "�� ������";
    [TextArea(3, 5)]
    public string description = "�����ٿ� ���� �����Դϴ�.";
    public Sprite icon;
    public int cost = 1; // ������ ���� ��� (��: �ൿ��)

    [Header("���� ȿ��")]
    [Range(0f, 1f)] // 0.0 (0%) ~ 1.0 (100%)
    public float baseSuccessRate = 0.7f; // �⺻ ���� Ȯ��

    // �� �������� �������� �� �ַ� � �ɷ�ġ�� �󸶳� �ø� ���ΰ�
    public StatType primaryTargetStat = StatType.None; // �ַ� ������ �ִ� �ɷ�ġ Ÿ��
    public int primaryStatImprovementAmount = 5;   // �ش� �ɷ�ġ ��� ���� (���� ��)

    public int stressChangeOnSuccess = 2;      // ���� �� ��Ʈ���� ��ȭ��

    // �������� ���� ȿ��
    // public StatType statPenaltyTargetStat = StatType.None; // ���� �� ������ �� ���� (�ʿ��ϴٸ�)
    // public int statPenaltyAmount = 0;       // ���� �� ���� ���ҷ� (0 �Ǵ� ����)
    public int stressChangeOnFailure = 10;     // ���� �� ��Ʈ���� ��ȭ��

    // TODO: ����/���� ȿ�� �ʵ�� ���� �ܰ迡�� �߰�
}