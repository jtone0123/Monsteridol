using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.PlayerLoop;

public class StatUI : MonoBehaviour
{
    public TextMeshProUGUI idolStatsText;

    // ǥ���� ���̵��� ������ (�� �����ʹ� �ܺο��� �������־�� �մϴ�)
    public IdolCharacter currentIdol;

    void Update()
    {
        // ���̵��� ������ UI�� ǥ��
        if (idolStatsText != null && currentIdol != null)
        {
            idolStatsText.text = $"name: {currentIdol.characterName}\n" +
                                 $"vocal: {currentIdol.stats[StatType.Vocal]}\n" +
                                 $"dance: {currentIdol.stats[StatType.Dance]}\n" +
                                 $"rap: {currentIdol.stats[StatType.Rap]}\n";
                                 
        }
    }
    

    
}

