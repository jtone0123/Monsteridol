using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections; // 코루틴 사용 시 필요 (여기서는 Update 사용)

public class DraggableScheduleItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField]
    public ScheduleData scheduleData;
    public RectTransform RectTransform { get; private set; }
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Canvas rootCanvas;

    private bool isBeingDragged = false;
    private ScheduleDropZone currentDropZoneTarget = null;

    [Header("꾹 눌러 설명 보기 설정")]
    [Tooltip("꾹 누르기로 인정할 최소 시간 (초)")]
    public float pressAndHoldDuration = 0.5f; // 0.5초 동안 누르고 있으면 설명 표시

    private bool isPointerDown = false;      // 현재 포인터가 눌린 상태인지
    private float pointerDownTimer = 0f;     // 포인터가 눌린 시간
    private bool isDescriptionCurrentlyShown = false; // 이 아이템에 의해 설명이 표시되었는지

    public string ItemNameDebug
    {
        get { return scheduleData != null ? scheduleData.scheduleName : gameObject.name; }
    }

    void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Update()
    {
        // "꾹 누르기" 로직 처리
        if (isPointerDown && !isBeingDragged && !isDescriptionCurrentlyShown)
        {
            pointerDownTimer += Time.deltaTime;
            if (pointerDownTimer >= pressAndHoldDuration)
            {
                ShowItemDescription();
            }
        }
    }

    private void ShowItemDescription()
    {
        if (scheduleData != null && DescriptionUIManager.Instance != null)
        {
            Debug.Log($"[{ItemNameDebug}] 꾹 누르기 성공! 설명 표시.");
            DescriptionUIManager.Instance.ShowDescription(scheduleData, RectTransform);
            isDescriptionCurrentlyShown = true;
        }
    }

    private void HideItemDescription()
    {
        if (isDescriptionCurrentlyShown && DescriptionUIManager.Instance != null)
        {
            DescriptionUIManager.Instance.HideDescription();
            isDescriptionCurrentlyShown = false;
            Debug.Log($"[{ItemNameDebug}] 설명 숨김.");
        }
        // 타이머 및 상태 초기화는 OnPointerUp 등에서 명시적으로 처리
    }

    private void ResetPressAndHold()
    {
        isPointerDown = false;
        pointerDownTimer = 0f;
        // isDescriptionCurrentlyShown은 HideItemDescription에서 관리
    }

    // --- IPointerDownHandler 구현 ---
    public void OnPointerDown(PointerEventData eventData)
    {
        // 드래그가 아닌, 단순 클릭 또는 꾹 누르기 시작일 수 있음
        if (!isBeingDragged) // 이미 드래그 중이 아닐 때만
        {
            isPointerDown = true;
            pointerDownTimer = 0f;
            // isDescriptionCurrentlyShown = false; // 여기서 false로 하면, 설명 본 직후 다시 못 볼 수 있음. Hide에서 처리.
            Debug.Log($"[{ItemNameDebug}] OnPointerDown");
        }
    }

    // --- IPointerUpHandler 구현 ---
    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"[{ItemNameDebug}] OnPointerUp");
        ResetPressAndHold();
        HideItemDescription(); // 손을 떼면 설명 숨김
    }

    // --- IPointerExitHandler 구현 (선택 사항이지만 유용) ---
    public void OnPointerExit(PointerEventData eventData)
    {
        // 누른 상태로 카드 영역을 벗어났다면 "꾹 누르기" 취소 및 설명 숨김
        if (isPointerDown) // isBeingDragged 조건은 필요 없음, 드래그 중에도 영역 벗어날 수 있음
        {
            Debug.Log($"[{ItemNameDebug}] OnPointerExit while pointer down. Cancelling press and hold.");
            ResetPressAndHold();
            HideItemDescription();
        }
    }


    // --- IBeginDragHandler 구현 ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (RectTransform == null) RectTransform = GetComponent<RectTransform>();
        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas == null)
        {
            Debug.LogError($"[{ItemNameDebug}] 최상위 Canvas를 찾을 수 없어 드래그를 시작할 수 없습니다.");
            eventData.pointerDrag = null;
            return;
        }

        Debug.Log($"[{ItemNameDebug}] OnBeginDrag 시작 from {transform.parent.name}");

        // 드래그가 시작되면 "꾹 누르기" 관련 상태를 모두 초기화하고 설명 숨김
        ResetPressAndHold();
        HideItemDescription(); // 매우 중요! 드래그 시작 시 설명 즉시 숨김

        isBeingDragged = true; // isPointerDown보다 먼저 true로 설정되어야 Update 로직이 꼬이지 않음
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;

        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();

        originalParent.GetComponent<ScheduleDropZone>()?.NotifyItemDragStarted(this, originalSiblingIndex);
    }

    // --- IDragHandler 구현 ---
    public void OnDrag(PointerEventData eventData)
    {
        if (!isBeingDragged || rootCanvas == null) return;

        RectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;

        ScheduleDropZone newHoveredDropZone = null;
        GameObject pointerOverObject = eventData.pointerCurrentRaycast.gameObject;
        if (pointerOverObject != null)
        {
            newHoveredDropZone = pointerOverObject.GetComponent<ScheduleDropZone>();
            if (newHoveredDropZone == null && pointerOverObject.transform.parent != null)
            {
                newHoveredDropZone = pointerOverObject.transform.parent.GetComponent<ScheduleDropZone>();
            }
        }

        if (currentDropZoneTarget != newHoveredDropZone)
        {
            currentDropZoneTarget?.NotifyItemDragExited(this);
            newHoveredDropZone?.NotifyItemDragEntered(this);
            currentDropZoneTarget = newHoveredDropZone;
        }

        currentDropZoneTarget?.NotifyItemDraggingOver(this, eventData.position);
    }

    // --- IEndDragHandler 구현 ---
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isBeingDragged) return; // 이미 드래그가 끝났거나 시작되지 않았다면 무시

        // isBeingDragged를 먼저 false로 설정해야, OnPointerUp 등이 호출될 때 올바르게 동작
        isBeingDragged = false;
        // ResetPressAndHold(); // OnPointerUp에서 이미 처리됨, 또는 여기서 한 번 더 호출해도 무방

        Debug.Log($"[{ItemNameDebug}] OnEndDrag 종료");

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        ScheduleDropZone finalDropZone = currentDropZoneTarget;

        if (finalDropZone != null && finalDropZone.isQueueDropZone)
        {
            Debug.Log($"[{ItemNameDebug}] 아이템이 '{finalDropZone.name}' (Queue Zone)에 드롭됨.");
            finalDropZone.HandleItemDrop(this);
        }
        else
        {
            Debug.Log($"[{ItemNameDebug}] 아이템이 유효하지 않은 곳 또는 비큐 존에 드롭되어 원래 위치로 복귀.");
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
            originalParent.GetComponent<ScheduleDropZone>()?.RefreshLayout(false);
        }

        currentDropZoneTarget?.NotifyItemDragExited(this);
        currentDropZoneTarget = null;
    }
}
