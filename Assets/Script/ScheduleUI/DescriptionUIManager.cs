using UnityEngine;
using TMPro; // TextMeshProUGUI 사용 시. 일반 Text 사용 시 UnityEngine.UI
using UnityEngine.UI; // CanvasScaler 참조 등을 위해 추가

public class DescriptionUIManager : MonoBehaviour
{
    public static DescriptionUIManager Instance { get; private set; }

    [Header("UI 연결")]
    [Tooltip("설명을 표시할 TextMeshProUGUI 또는 Text 컴포넌트")]
    public TextMeshProUGUI descriptionTextUI; // Inspector에서 연결

    [Tooltip("설명 UI 전체를 감싸는 GameObject (활성화/비활성화 제어용)")]
    public GameObject descriptionPanel; // Inspector에서 연결

    [Header("패널 위치 설정")]
    [Tooltip("스케줄로부터 설명 패널이 떨어질 X축 오프셋")]
    public float offsetX = 10f;
    [Tooltip("스케줄로부터 설명 패널이 떨어질 Y축 오프셋")]
    public float offsetY = 0f;
    [Tooltip("설명 패널이 화면 가장자리에서 얼마나 떨어져야 하는지 (경계 보정용)")]
    public float screenPadding = 10f;


    private RectTransform panelRectTransform;
    private Canvas mainCanvas; // 패널 위치 계산을 위한 최상위 Canvas 참조

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (descriptionPanel != null)
        {
            panelRectTransform = descriptionPanel.GetComponent<RectTransform>();
            if (panelRectTransform == null)
            {
                Debug.LogError("DescriptionUIManager: descriptionPanel에 RectTransform 컴포넌트가 없습니다!");
            }

            // 최상위 Canvas 찾기 (Screen Space Overlay 가정)
            // 1. 현재 게임 오브젝트 또는 부모에서 Canvas를 먼저 찾아봅니다.
            mainCanvas = GetComponentInParent<Canvas>();

            // 2. GetComponentInParent로 찾지 못했고, Screen Space Overlay가 아니라면
            //    씬에서 CanvasScaler를 가진 활성화된 Canvas를 찾아봅니다. (최후의 수단)
            if (mainCanvas == null || mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                // FindFirstObjectByType 사용 (FindObjectOfType 대체)
                CanvasScaler scaler = FindFirstObjectByType<CanvasScaler>();
                if (scaler != null)
                {
                    mainCanvas = scaler.GetComponent<Canvas>();
                }

                if (mainCanvas == null || mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    Debug.LogError("DescriptionUIManager: Screen Space Overlay 모드의 최상위 Canvas를 찾을 수 없습니다. 패널 위치 계산에 문제가 있을 수 있습니다. Inspector에서 mainCanvas를 직접 할당하거나, 이 오브젝트가 올바른 Canvas의 자식인지 확인해주세요.");
                }
            }
            descriptionPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("DescriptionUIManager: 설명 UI 요소(descriptionPanel)가 Inspector에 연결되지 않았습니다!");
        }
    }

    /// <summary>
    /// 지정된 ScheduleData의 설명을 특정 스케줄 옆에 UI로 표시합니다.
    /// </summary>
    /// <param name="dataToShow">표시할 스케줄 데이터</param>
    /// <param name="targetScheduleRect">설명 패널의 기준이 될 스케줄의 RectTransform</param>
    public void ShowDescription(ScheduleData dataToShow, RectTransform targetScheduleRect)
    {
        if (dataToShow == null || panelRectTransform == null || descriptionTextUI == null || mainCanvas == null)
        {
            // ... (null 체크 및 경고 로그는 이전과 동일) ...
            HideDescription();
            return;
        }

        // 1. 설명 텍스트 설정
        string displayText = $"<size=40>{dataToShow.description}</size>\n\n";
        
    
        descriptionTextUI.text = displayText;

        // 2. 설명 패널 크기 강제 업데이트 (ContentSizeFitter 등이 내용에 맞게 크기를 조절한 후 위치 계산)
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRectTransform);

        // 3. 설명 패널 위치 계산 (이하 로직은 이전과 동일)
        Vector3[] scheduleWorldCorners = new Vector3[4];
        targetScheduleRect.GetWorldCorners(scheduleWorldCorners);

        Vector3 panelTargetWorldPos;
        bool showOnRightSide = false;

        Vector3 scheduleLeftCenterWorld = (scheduleWorldCorners[0] + scheduleWorldCorners[1]) / 2f;
        Vector3 scheduleRightCenterWorld = (scheduleWorldCorners[2] + scheduleWorldCorners[3]) / 2f;

        float panelWidth = panelRectTransform.rect.width * mainCanvas.scaleFactor;
        float panelHeight = panelRectTransform.rect.height * mainCanvas.scaleFactor;

        panelTargetWorldPos = new Vector3(
            scheduleLeftCenterWorld.x - ((1f - panelRectTransform.pivot.x) * panelWidth) - offsetX * mainCanvas.scaleFactor,
            scheduleLeftCenterWorld.y - ((0.5f - panelRectTransform.pivot.y) * panelHeight) + offsetY * mainCanvas.scaleFactor,
            0);

        if (panelTargetWorldPos.x - (panelRectTransform.pivot.x * panelWidth) < screenPadding)
        {
            showOnRightSide = true;
        }

        if (showOnRightSide)
        {
            panelTargetWorldPos = new Vector3(
                scheduleRightCenterWorld.x + (panelRectTransform.pivot.x * panelWidth) + offsetX * mainCanvas.scaleFactor,
                scheduleRightCenterWorld.y - ((0.5f - panelRectTransform.pivot.y) * panelHeight) + offsetY * mainCanvas.scaleFactor,
                0);

            if (panelTargetWorldPos.x + (1f - panelRectTransform.pivot.x) * panelWidth > Screen.width - screenPadding)
            {
                panelTargetWorldPos.x = Screen.width - screenPadding - ((1f - panelRectTransform.pivot.x) * panelWidth);
            }
        }

        float panelTopY = panelTargetWorldPos.y + (1f - panelRectTransform.pivot.y) * panelHeight;
        float panelBottomY = panelTargetWorldPos.y - panelRectTransform.pivot.y * panelHeight;

        if (panelTopY > Screen.height - screenPadding)
        {
            panelTargetWorldPos.y -= (panelTopY - (Screen.height - screenPadding));
        }
        if (panelBottomY < screenPadding)
        {
            panelTargetWorldPos.y += (screenPadding - panelBottomY);
        }

        panelRectTransform.position = panelTargetWorldPos;

        descriptionPanel.SetActive(true);
        Debug.Log($"'{dataToShow.scheduleName}' 스케줄 설명 표시 (기준 스케줄 옆).");
    }

    public void HideDescription()
    {
        if (descriptionPanel != null && descriptionPanel.activeSelf)
        {
            descriptionPanel.SetActive(false);
            Debug.Log("스케줄 설명 UI 숨김.");
        }
        else if (descriptionTextUI != null && descriptionTextUI.gameObject.activeSelf)
        {
            descriptionTextUI.gameObject.SetActive(false);
        }
    }
}
