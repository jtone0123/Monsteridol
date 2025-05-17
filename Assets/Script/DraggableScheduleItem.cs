using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Unity�� �̺�Ʈ �ý��� ����� ���� �ʼ�!

public class DraggableScheduleItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    public ScheduleData scheduleData;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;

    // (���� ����) � ���������� �α׿��� �����ϱ� ���� ������ �̸�. ScheduleData ���� ���.
    public string itemNameForDebug = "DraggableItem";

    private GameObject placeholder = null; // �ڸ� ǥ���� ���� ������Ʈ
    private Transform placeholderParentForDrag = null;// placeholderParentForDrag�� placeholder�� �������� �Űܴٴ� �θ� ����.

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // CanvasGroup�� ���ٸ� ��� (�����տ� �̸� �߰��ϴ� ���� ����)
        if (canvasGroup == null)
        {
            Debug.LogErrorFormat("DraggableScheduleItem: '{0}' ������Ʈ�� CanvasGroup ������Ʈ�� �����ϴ�. �߰����ּ���.", gameObject.name);
            canvasGroup = gameObject.AddComponent<CanvasGroup>(); // �ʿ�� ���� �߰�
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"{itemNameForDebug} ({gameObject.name}): �巡�� ����");

        originalParent = transform.parent; // ���� �θ� ���

        // --- Placeholder ���� ���� ���� ---
        placeholder = new GameObject();
        placeholder.name = "Placeholder";
        placeholder.transform.SetParent(originalParent); // �ϴ� ���� �������� �ִ� �гο� ����
        placeholder.transform.SetSiblingIndex(transform.GetSiblingIndex()); // ���� �������� ������ ����

        // Placeholder�� Image ������Ʈ �߰��Ͽ� ���̰� ����� (��: ���� ȸ��)
        Image placeholderImage = placeholder.AddComponent<Image>();
        placeholderImage.color = new Color(0.8f, 0.8f, 0.8f, 0.5f); // ���� ȸ��, �ణ �����ϰ�
                                                                    // placeholderImage.raycastTarget = false; // ���콺 �̺�Ʈ�� ���� �ʵ���

        LayoutElement phLE = placeholder.AddComponent<LayoutElement>();
        LayoutElement itemLE = GetComponent<LayoutElement>();

        if (itemLE != null) // <<--- ������ LayoutElement�� �ִ� ���
        {
            phLE.preferredWidth = itemLE.preferredWidth;
            phLE.preferredHeight = itemLE.preferredHeight;
            
            phLE.flexibleWidth = itemLE.flexibleWidth;
            phLE.flexibleHeight = itemLE.flexibleHeight;
            // phLE.minWidth = itemLE.minWidth; // �ʿ��ϴٸ� �ּ�/�ִ� ũ�⵵ ����
            // phLE.minHeight = itemLE.minHeight;
        }
        else // <<--- ������ LayoutElement�� ���� ��� (RectTransform ũ�� ���)
        {
            RectTransform itemRect = GetComponent<RectTransform>();
            phLE.preferredWidth = itemRect.rect.width;
            phLE.preferredHeight = itemRect.rect.height;
            // �� ��� flexible �� ���� �⺻��(0 �Ǵ� -1)���� ������ �� ����
        }
        // --- Placeholder ���� ���� �� ---

        Debug.Log($"Placeholder Size Set: PreferredWidth = {phLE.preferredWidth}, PreferredHeight = {phLE.preferredHeight}");
        if (itemLE != null)
        {
            Debug.Log($"Original Item LayoutElement: PrefWidth={itemLE.preferredWidth}, PrefHeight={itemLE.preferredHeight}, FlexWidth={itemLE.flexibleWidth}");
        }
        else
        {
            RectTransform itemRect = GetComponent<RectTransform>();
            Debug.Log($"Original Item RectTransform: Width={itemRect.rect.width}, Height={itemRect.rect.height}");
        }

        placeholderParentForDrag = originalParent;

        placeholderParentForDrag = originalParent;
        // �巡�� ���� �������� �ٸ� UI ��ҵ� ���� �׷������� Canvas�� ���� �ڽ����� ����
        transform.SetParent(GetComponentInParent<Canvas>().transform, true);
        transform.SetAsLastSibling(); // ���� �������� �׷������� (��, ���� ����)
        //���߿� �����ڸ���

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false; // �߿�: �巡�� ���� ������ �ڽ��� ���콺 �̺�Ʈ�� ����ä�� �ʵ���
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // ���콺 �����͸� ���� UI ������ �̵�
        rectTransform.anchoredPosition += eventData.delta / GetComponentInParent<Canvas>().scaleFactor;
        if (placeholder == null) return;

        Transform currentHoveredDropZoneTransform = null;
        GameObject pointerOverObject = eventData.pointerCurrentRaycast.gameObject;

        if (pointerOverObject)
        {
            ScheduleDropZone dropZoneScript = pointerOverObject.GetComponent<ScheduleDropZone>();
            if (dropZoneScript) // ���콺�� DropZone ���� ���� ���� ��
            {
                currentHoveredDropZoneTransform = dropZoneScript.transform;
            }
            else if (currentHoveredDropZoneTransform) // ���콺�� DropZone ���� �ٸ� ������ ���� ���� ��
            {
                dropZoneScript = pointerOverObject.transform.parent.GetComponent<ScheduleDropZone>();

                if (dropZoneScript)
                {
                    currentHoveredDropZoneTransform = dropZoneScript.transform;
                }
            }
        }

        if (currentHoveredDropZoneTransform != null)
        {
            placeholderParentForDrag = currentHoveredDropZoneTransform;
            if (placeholder.transform.parent != placeholderParentForDrag)
            {              
                placeholder.transform.SetParent(placeholderParentForDrag);
               
            }
           
            
            
            int newSiblingIndex = placeholderParentForDrag.childCount;
            if (placeholder.transform.parent == currentHoveredDropZoneTransform)
            {
                newSiblingIndex = currentHoveredDropZoneTransform.childCount - 1; // �̹� �ڽ��̸� �� �ڿ��� �ϳ� �� (�ڽ� ����)
            }
            else
            {
                newSiblingIndex = currentHoveredDropZoneTransform.childCount; // ���� ������ �� ��
            }



            for (int i = 0; i < currentHoveredDropZoneTransform.childCount; i++)
            {
                Transform child = currentHoveredDropZoneTransform.GetChild(i);
                if (child == placeholder.transform) continue;

                // child�� RectTransform ��������
                RectTransform childRect = child.GetComponent<RectTransform>();
                if (childRect == null) continue;

                Vector3[] childCorners = new Vector3[4];
                childRect.GetWorldCorners(childCorners); // 0: BottomLeft, 1: TopLeft, 2: TopRight, 3: BottomRight
                float childTopY = childCorners[1].y;
                float childBottomY = childCorners[0].y;
                float childCenterY = (childTopY + childBottomY) / 2f;


                if (transform.position.y > childCenterY) // ���� �������� child�� �߽ɺ��� ���� �ִٸ�
                {
                    newSiblingIndex = i;
                    break;
                }
                // -----------------------------------------------------------
            }
            placeholder.transform.SetSiblingIndex(newSiblingIndex);
        }
        else // ��ȿ�� DropZone ���� ���� �ʴٸ�
        {
            placeholderParentForDrag = originalParent;
            if (placeholder.transform.parent != originalParent)
            {              
                placeholder.transform.SetParent(originalParent);
                
            }
          
            // originalParent�� ���ư��� �� placeholder�� ������ ��� ���� ����
            // ��: placeholder.transform.SetAsLastSibling(); �Ǵ� ���� sibling index (�����ص״ٸ�)
        }
    
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"{itemNameForDebug} ({gameObject.name}): �巡�� ����");

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // Placeholder�� ���������� ��ġ�� �θ�� ������ ���� �������� ����
        if (placeholder != null && placeholder.transform.parent != null) // Placeholder�� ��ȿ�� �θ� ������ �ִٸ�
        {
            transform.SetParent(placeholder.transform.parent); // ���� �������� �θ� Placeholder�� �θ�� ����
            transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex()); // ���� �������� ������ Placeholder�� ������ ����
        }
        else // Placeholder�� �θ� � �����ε� ��ȿ���� �ʴٸ� (��: �巡�� �߿� ������ ������ originalParent�� ���ư� ��� ��)
        {
            transform.SetParent(originalParent); // �������� ���� �θ�� ����
                                                 // �ʿ��ϴٸ� ���� ������ ���� (originalSiblingIndex ��� - OnBeginDrag���� �����ؾ� ��)
        }

        // Placeholder�� �� �̻� �ʿ� �����Ƿ� ����
        if (placeholder != null)
        {
            Destroy(placeholder);
        }
        placeholder = null; // ������ ����ϰ� ����
        placeholderParentForDrag = null;
    }
}