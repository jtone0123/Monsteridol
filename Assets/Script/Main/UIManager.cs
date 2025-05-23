// UIManager.cs
// Unity UI 패널 전환을 관리하는 스크립트입니다.
// 이 스크립트를 Canvas나 빈 GameObject("UIManager" 등)에 추가합니다.

using UnityEngine;
using UnityEngine.UI; // Button과 같은 UI 요소에 접근하려면 필요할 수 있습니다.
using TMPro;

public class UIManager : MonoBehaviour
{
    // Inspector 창에서 연결할 패널들
    [Header("UI Panels")] // Inspector에서 보기 좋게 그룹화
    public GameObject mainMenuPanel;          // 메인 메뉴 패널
    public GameObject scheduleManagementPanel; // 스케줄 관리 패널
   // public TextMeshProUGUI currentTurnText; // 현재 턴 텍스트 UI
    // 필요에 따라 다른 패널들도 여기에 추가할 수 있습니다.
    // public GameObject idolDetailPanel;
    // public GameObject settingsPanel;
    //int currentTurn = 0;

    //public void CurrentTurnUpdate()
    //{
     //   currentTurn++;
     //   currentTurnText.text = $"<size=200>{currentTurn}</size>";
   // }
    void Start()
    {

        //CurrentTurnUpdate();

        // 게임 시작 시 초기 상태 설정: 메인 메뉴만 활성화
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("메인 메뉴 패널이 UIManager에 할당되지 않았습니다!");
        }

        if (scheduleManagementPanel != null)
        {
            scheduleManagementPanel.SetActive(false); // 스케줄 관리 패널은 처음에는 비활성화
        }
        else
        {
            Debug.LogError("스케줄 관리 패널이 UIManager에 할당되지 않았습니다!");
        }

        // 다른 패널들도 초기에는 비활성화
        // if (idolDetailPanel != null) idolDetailPanel.SetActive(false);
        // if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // 스케줄 관리 패널을 보여주는 함수
    // 이 함수를 메인 메뉴의 "스케줄 설정" 버튼의 OnClick 이벤트에 연결합니다.
    public void ShowScheduleManagementPanel()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false); // 메인 메뉴 패널 숨기기
        }

        if (scheduleManagementPanel != null)
        {
            scheduleManagementPanel.SetActive(true); // 스케줄 관리 패널 보이기
        }
        else
        {
            Debug.LogError("스케줄 관리 패널을 찾을 수 없습니다. UIManager에 할당되었는지 확인해주세요.");
        }
    }

    // 메인 메뉴 패널을 보여주는 함수
    // 이 함수는 스케줄 관리 패널의 "뒤로가기" 또는 "메인으로" 버튼의 OnClick 이벤트에 연결할 수 있습니다.
    public void ShowMainMenuPanel()
    {
        if (scheduleManagementPanel != null)
        {
            scheduleManagementPanel.SetActive(false); // 스케줄 관리 패널 숨기기
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true); // 메인 메뉴 패널 보이기
        }
        else
        {
            Debug.LogError("메인 메뉴 패널을 찾을 수 없습니다. UIManager에 할당되었는지 확인해주세요.");
        }
    }

    // (선택 사항) 다른 패널들을 위한 함수들
    // public void ShowIdolDetailPanel()
    // {
    //     if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    //     if (idolDetailPanel != null) idolDetailPanel.SetActive(true);
    // }

    // public void ShowSettingsPanel()
    // {
    //     if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    //     if (settingsPanel != null) settingsPanel.SetActive(true);
    // }

    // 모든 활성 패널을 닫고 메인 메뉴로 돌아가는 함수 (일괄 처리용)
    public void CloseAllPanelsAndShowMain()
    {
        if (scheduleManagementPanel != null) scheduleManagementPanel.SetActive(false);
        // if (idolDetailPanel != null) idolDetailPanel.SetActive(false);
        // if (settingsPanel != null) settingsPanel.SetActive(false);

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("메인 메뉴 패널을 찾을 수 없습니다. UIManager에 할당되었는지 확인해주세요.");
        }
    }
}
