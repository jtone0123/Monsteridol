using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // CanvasGroup 사용

public class DraggableScheduleItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    public ScheduleData scheduleData; // 이 아이템이 나타내는 스케줄 데이터
    public RectTransform RectTransform { get; private set; }
    private CanvasGroup canvasGroup;
    private Transform originalParent;       // 드래그 시작 전 원래 부모 Transform
    private int originalSiblingIndex;   // 드래그 시작 전 원래 Sibling Index
    private Canvas rootCanvas;          // 드래그 시 아이템이 속할 최상위 Canvas

    private bool isBeingDragged = false; // 현재 이 아이템이 드래그 되고 있는지 여부
    private ScheduleDropZone currentDropZoneTarget = null; // 현재 마우스/터치 포인터가 위에 있는 ScheduleDropZone

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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (RectTransform == null) RectTransform = GetComponent<RectTransform>();
        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas == null)
        {
            Debug.LogError($"[{ItemNameDebug}] 최상위 Canvas를 찾을 수 없어 드래그를 시작할 수 없습니다.");
            eventData.pointerDrag = null; // 드래그 취소
            return;
        }

        Debug.Log($"[{ItemNameDebug}] OnBeginDrag 시작. 원래 부모: {transform.parent.name}, 원래 순서: {transform.GetSiblingIndex()}");

        isBeingDragged = true;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false; // 자신이 레이캐스트를 막지 않도록

        // 아이템을 최상위 Canvas로 이동시켜 다른 UI 위에 보이도록 함
        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();

        // 원래 부모였던 DropZone에 알림 (Placeholder 생성 등을 위해)
        originalParent.GetComponent<ScheduleDropZone>()?.NotifyItemDragStarted(this, originalSiblingIndex);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isBeingDragged || rootCanvas == null) return;

        RectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;

        // 현재 마우스 아래 DropZone 감지
        ScheduleDropZone newDropZoneTarget = null;
        GameObject pointerOverObject = eventData.pointerCurrentRaycast.gameObject;
        if (pointerOverObject != null)
        {
            newDropZoneTarget = pointerOverObject.GetComponent<ScheduleDropZone>();
            if (newDropZoneTarget == null && pointerOverObject.transform.parent != null)
            {
                newDropZoneTarget = pointerOverObject.transform.parent.GetComponent<ScheduleDropZone>();
            }
        }

        if (currentDropZoneTarget != newDropZoneTarget)
        {
            // 이전 DropZone에서 나감을 알림
            currentDropZoneTarget?.NotifyItemDragExited(this);
            // 새 DropZone에 진입함을 알림
            newDropZoneTarget?.NotifyItemDragEntered(this);
            currentDropZoneTarget = newDropZoneTarget;
        }

        // 현재 DropZone 위에서 계속 드래그 중임을 알림 (마우스 위치 전달)
        currentDropZoneTarget?.NotifyItemDraggingOver(this, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isBeingDragged) return;
        isBeingDragged = false;

        Debug.Log($"[{ItemNameDebug}] OnEndDrag 종료");

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        ScheduleDropZone finalDropZone = currentDropZoneTarget; // OnDrag에서 마지막으로 감지된 DropZone

        if (finalDropZone != null && finalDropZone.isQueueDropZone) // isQueueDropZone은 ScheduleDropZone에 정의
        {
            Debug.Log($"[{ItemNameDebug}] 아이템이 '{finalDropZone.name}' (Queue Zone)에 드롭됨.");
            finalDropZone.HandleItemDrop(this); // DropZone이 아이템 배치 및 Placeholder 제거 처리
        }
        else
        {
            Debug.Log($"[{ItemNameDebug}] 아이템이 유효하지 않은 곳 또는 비큐 존에 드롭되어 원래 위치로 복귀.");
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
            // 원래 부모였던 DropZone에 아이템 복귀 후 레이아웃 갱신 요청
            originalParent.GetComponent<ScheduleDropZone>()?.RefreshLayout(false); // 애니메이션 없이 즉시
        }

        // 드래그가 끝났으므로, 현재 아이템이 어떤 DropZone 위에 있든 Exited 알림을 보내서
        // 해당 DropZone이 Placeholder 등을 정리하도록 함
        currentDropZoneTarget?.NotifyItemDragExited(this);
        currentDropZoneTarget = null;
    }
}
