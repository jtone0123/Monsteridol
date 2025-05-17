using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Unity의 이벤트 시스템 사용을 위해 필수!

public class DraggableScheduleItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    public ScheduleData scheduleData;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;

    // (선택 사항) 어떤 아이템인지 로그에서 구분하기 위한 간단한 이름. ScheduleData 없이 사용.
    public string itemNameForDebug = "DraggableItem";

    private GameObject placeholder = null; // 자리 표시자 게임 오브젝트
    private Transform placeholderParentForDrag = null;// placeholderParentForDrag는 placeholder가 동적으로 옮겨다닐 부모를 추적.

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // CanvasGroup이 없다면 경고 (프리팹에 미리 추가하는 것이 좋음)
        if (canvasGroup == null)
        {
            Debug.LogErrorFormat("DraggableScheduleItem: '{0}' 오브젝트에 CanvasGroup 컴포넌트가 없습니다. 추가해주세요.", gameObject.name);
            canvasGroup = gameObject.AddComponent<CanvasGroup>(); // 필요시 강제 추가
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"{itemNameForDebug} ({gameObject.name}): 드래그 시작");

        originalParent = transform.parent; // 현재 부모를 기억

        // --- Placeholder 생성 로직 시작 ---
        placeholder = new GameObject();
        placeholder.name = "Placeholder";
        placeholder.transform.SetParent(originalParent); // 일단 원래 아이템이 있던 패널에 생성
        placeholder.transform.SetSiblingIndex(transform.GetSiblingIndex()); // 원래 아이템의 순서에 생성

        // Placeholder에 Image 컴포넌트 추가하여 보이게 만들기 (예: 연한 회색)
        Image placeholderImage = placeholder.AddComponent<Image>();
        placeholderImage.color = new Color(0.8f, 0.8f, 0.8f, 0.5f); // 연한 회색, 약간 투명하게
                                                                    // placeholderImage.raycastTarget = false; // 마우스 이벤트는 받지 않도록

        LayoutElement phLE = placeholder.AddComponent<LayoutElement>();
        LayoutElement itemLE = GetComponent<LayoutElement>();

        if (itemLE != null) // <<--- 원본에 LayoutElement가 있는 경우
        {
            phLE.preferredWidth = itemLE.preferredWidth;
            phLE.preferredHeight = itemLE.preferredHeight;
            
            phLE.flexibleWidth = itemLE.flexibleWidth;
            phLE.flexibleHeight = itemLE.flexibleHeight;
            // phLE.minWidth = itemLE.minWidth; // 필요하다면 최소/최대 크기도 복사
            // phLE.minHeight = itemLE.minHeight;
        }
        else // <<--- 원본에 LayoutElement가 없는 경우 (RectTransform 크기 사용)
        {
            RectTransform itemRect = GetComponent<RectTransform>();
            phLE.preferredWidth = itemRect.rect.width;
            phLE.preferredHeight = itemRect.rect.height;
            // 이 경우 flexible 값 등은 기본값(0 또는 -1)으로 설정될 수 있음
        }
        // --- Placeholder 생성 로직 끝 ---

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
        // 드래그 중인 아이템이 다른 UI 요소들 위에 그려지도록 Canvas의 직접 자식으로 만듦
        transform.SetParent(GetComponentInParent<Canvas>().transform, true);
        transform.SetAsLastSibling(); // 가장 마지막에 그려지도록 (즉, 가장 위에)
        //나중엔 원래자리로

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false; // 중요: 드래그 중인 아이템 자신이 마우스 이벤트를 가로채지 않도록
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 마우스 포인터를 따라 UI 아이템 이동
        rectTransform.anchoredPosition += eventData.delta / GetComponentInParent<Canvas>().scaleFactor;
        if (placeholder == null) return;

        Transform currentHoveredDropZoneTransform = null;
        GameObject pointerOverObject = eventData.pointerCurrentRaycast.gameObject;

        if (pointerOverObject)
        {
            ScheduleDropZone dropZoneScript = pointerOverObject.GetComponent<ScheduleDropZone>();
            if (dropZoneScript) // 마우스가 DropZone 위에 직접 있을 때
            {
                currentHoveredDropZoneTransform = dropZoneScript.transform;
            }
            else if (currentHoveredDropZoneTransform) // 마우스가 DropZone 안의 다른 아이템 위에 있을 때
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
                newSiblingIndex = currentHoveredDropZoneTransform.childCount - 1; // 이미 자식이면 맨 뒤에서 하나 앞 (자신 포함)
            }
            else
            {
                newSiblingIndex = currentHoveredDropZoneTransform.childCount; // 새로 들어오면 맨 뒤
            }



            for (int i = 0; i < currentHoveredDropZoneTransform.childCount; i++)
            {
                Transform child = currentHoveredDropZoneTransform.GetChild(i);
                if (child == placeholder.transform) continue;

                // child의 RectTransform 가져오기
                RectTransform childRect = child.GetComponent<RectTransform>();
                if (childRect == null) continue;

                Vector3[] childCorners = new Vector3[4];
                childRect.GetWorldCorners(childCorners); // 0: BottomLeft, 1: TopLeft, 2: TopRight, 3: BottomRight
                float childTopY = childCorners[1].y;
                float childBottomY = childCorners[0].y;
                float childCenterY = (childTopY + childBottomY) / 2f;


                if (transform.position.y > childCenterY) // 현재 아이템이 child의 중심보다 위에 있다면
                {
                    newSiblingIndex = i;
                    break;
                }
                // -----------------------------------------------------------
            }
            placeholder.transform.SetSiblingIndex(newSiblingIndex);
        }
        else // 유효한 DropZone 위에 있지 않다면
        {
            placeholderParentForDrag = originalParent;
            if (placeholder.transform.parent != originalParent)
            {              
                placeholder.transform.SetParent(originalParent);
                
            }
          
            // originalParent로 돌아갔을 때 placeholder의 순서를 어떻게 할지 결정
            // 예: placeholder.transform.SetAsLastSibling(); 또는 원래 sibling index (저장해뒀다면)
        }
    
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"{itemNameForDebug} ({gameObject.name}): 드래그 종료");

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // Placeholder가 최종적으로 위치한 부모와 순서에 실제 아이템을 놓음
        if (placeholder != null && placeholder.transform.parent != null) // Placeholder가 유효한 부모를 가지고 있다면
        {
            transform.SetParent(placeholder.transform.parent); // 실제 아이템의 부모를 Placeholder의 부모로 설정
            transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex()); // 실제 아이템의 순서를 Placeholder의 순서로 설정
        }
        else // Placeholder의 부모가 어떤 이유로든 유효하지 않다면 (예: 드래그 중에 밖으로 나가서 originalParent로 돌아간 경우 등)
        {
            transform.SetParent(originalParent); // 아이템의 원래 부모로 복귀
                                                 // 필요하다면 원래 순서로 복귀 (originalSiblingIndex 사용 - OnBeginDrag에서 저장해야 함)
        }

        // Placeholder는 더 이상 필요 없으므로 제거
        if (placeholder != null)
        {
            Destroy(placeholder);
        }
        placeholder = null; // 참조도 깔끔하게 정리
        placeholderParentForDrag = null;
    }
}