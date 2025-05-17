using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // CanvasGroup ���

public class DraggableScheduleItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    public ScheduleData scheduleData; // �� �������� ��Ÿ���� ������ ������
    public RectTransform RectTransform { get; private set; }
    private CanvasGroup canvasGroup;
    private Transform originalParent;       // �巡�� ���� �� ���� �θ� Transform
    private int originalSiblingIndex;   // �巡�� ���� �� ���� Sibling Index
    private Canvas rootCanvas;          // �巡�� �� �������� ���� �ֻ��� Canvas

    private bool isBeingDragged = false; // ���� �� �������� �巡�� �ǰ� �ִ��� ����
    private ScheduleDropZone currentDropZoneTarget = null; // ���� ���콺/��ġ �����Ͱ� ���� �ִ� ScheduleDropZone

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
            Debug.LogError($"[{ItemNameDebug}] �ֻ��� Canvas�� ã�� �� ���� �巡�׸� ������ �� �����ϴ�.");
            eventData.pointerDrag = null; // �巡�� ���
            return;
        }

        Debug.Log($"[{ItemNameDebug}] OnBeginDrag ����. ���� �θ�: {transform.parent.name}, ���� ����: {transform.GetSiblingIndex()}");

        isBeingDragged = true;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false; // �ڽ��� ����ĳ��Ʈ�� ���� �ʵ���

        // �������� �ֻ��� Canvas�� �̵����� �ٸ� UI ���� ���̵��� ��
        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();

        // ���� �θ𿴴� DropZone�� �˸� (Placeholder ���� ���� ����)
        originalParent.GetComponent<ScheduleDropZone>()?.NotifyItemDragStarted(this, originalSiblingIndex);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isBeingDragged || rootCanvas == null) return;

        RectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;

        // ���� ���콺 �Ʒ� DropZone ����
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
            // ���� DropZone���� ������ �˸�
            currentDropZoneTarget?.NotifyItemDragExited(this);
            // �� DropZone�� �������� �˸�
            newDropZoneTarget?.NotifyItemDragEntered(this);
            currentDropZoneTarget = newDropZoneTarget;
        }

        // ���� DropZone ������ ��� �巡�� ������ �˸� (���콺 ��ġ ����)
        currentDropZoneTarget?.NotifyItemDraggingOver(this, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isBeingDragged) return;
        isBeingDragged = false;

        Debug.Log($"[{ItemNameDebug}] OnEndDrag ����");

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        ScheduleDropZone finalDropZone = currentDropZoneTarget; // OnDrag���� ���������� ������ DropZone

        if (finalDropZone != null && finalDropZone.isQueueDropZone) // isQueueDropZone�� ScheduleDropZone�� ����
        {
            Debug.Log($"[{ItemNameDebug}] �������� '{finalDropZone.name}' (Queue Zone)�� ��ӵ�.");
            finalDropZone.HandleItemDrop(this); // DropZone�� ������ ��ġ �� Placeholder ���� ó��
        }
        else
        {
            Debug.Log($"[{ItemNameDebug}] �������� ��ȿ���� ���� �� �Ǵ� ��ť ���� ��ӵǾ� ���� ��ġ�� ����.");
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
            // ���� �θ𿴴� DropZone�� ������ ���� �� ���̾ƿ� ���� ��û
            originalParent.GetComponent<ScheduleDropZone>()?.RefreshLayout(false); // �ִϸ��̼� ���� ���
        }

        // �巡�װ� �������Ƿ�, ���� �������� � DropZone ���� �ֵ� Exited �˸��� ������
        // �ش� DropZone�� Placeholder ���� �����ϵ��� ��
        currentDropZoneTarget?.NotifyItemDragExited(this);
        currentDropZoneTarget = null;
    }
}
