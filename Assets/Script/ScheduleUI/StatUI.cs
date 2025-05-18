using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.PlayerLoop;

public class StatUI : MonoBehaviour
{
    public TextMeshProUGUI idolStatsText;

    // 표시할 아이돌의 데이터 (이 데이터는 외부에서 설정해주어야 합니다)
    public IdolCharacter currentIdol;

    void Update()
    {
        // 아이돌의 스탯을 UI에 표시
        if (idolStatsText != null && currentIdol != null)
        {
            idolStatsText.text = $"name: {currentIdol.characterName}\n" +
                                 $"vocal: {currentIdol.stats[StatType.Vocal]}\n" +
                                 $"dance: {currentIdol.stats[StatType.Dance]}\n" +
                                 $"rap: {currentIdol.stats[StatType.Rap]}\n";
                                 
        }
    }
    

    
}

