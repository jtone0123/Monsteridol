using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections.Generic;

public class ScheduleDropZone : MonoBehaviour
{
    [Header("레이아웃 설정")]
    public bool isQueueDropZone = false;
    public float itemSpacing = 10f;
    public float topPadding = 10f;
    public float horizontalPadding = 20f;

    [Header("아이템 크기 설정")]
    public bool fitWidthToDropZone = true;

    [Header("애니메이션 설정")]
    public float siblingAnimationDuration = 0.2f;

    private GameObject placeholder = null;
    private RectTransform placeholderRT = null;
    private LayoutElement placeholderLE = null;

    private List<Tween> activeItemTweens = new List<Tween>();
    // private Transform lastValidDropZone = null; // 현재 직접적인 사용처가 명확하지 않아 주석 처리 또는 제거 고려

    private struct ItemLayoutState
    {
        public RectTransform rt;
        public Vector2 initialAnchoredPos;
        public ItemLayoutState(RectTransform rt, Vector2 pos)
        {
            this.rt = rt;
            this.initialAnchoredPos = pos;
        }
    }

    public void NotifyItemDragStarted(DraggableScheduleItem draggedItem, int originalIndex)
    {
        if (!isQueueDropZone) return;
        CreatePlaceholder(draggedItem, originalIndex);
        RefreshLayout(false);
    }

    public void NotifyItemDragEntered(DraggableScheduleItem draggedItem)
    {
        if (!isQueueDropZone) return;
        if (placeholder == null)
        {
            CreatePlaceholder(draggedItem, CalculatePlaceholderSiblingIndex(draggedItem.RectTransform.position));
        }
        if (!placeholder.activeSelf) placeholder.SetActive(true); // 필요시 활성화

        if (placeholder.transform.parent != transform)
        {
            placeholder.transform.SetParent(transform);
        }
        int newIndex = CalculatePlaceholderSiblingIndex(draggedItem.RectTransform.position);
        placeholder.transform.SetSiblingIndex(newIndex);
        RefreshLayout(true);
    }

    public void NotifyItemDraggingOver(DraggableScheduleItem draggedItem, Vector2 mouseScreenPosition)
    {
        if (!isQueueDropZone || placeholder == null || !placeholder.activeSelf) return;

        int newPlaceholderIndex = CalculatePlaceholderSiblingIndex(mouseScreenPosition);
        if (placeholder.transform.GetSiblingIndex() != newPlaceholderIndex)
        {
            placeholder.transform.SetSiblingIndex(newPlaceholderIndex);
            RefreshLayout(true);
        }
    }

    public void NotifyItemDragExited(DraggableScheduleItem draggedItem)
    {
        if (!isQueueDropZone) return;
        if (placeholder != null)
        {
            DestroyPlaceholder(); // 내부에서 SetActive(false) 후 Destroy
            RefreshLayout(true);
        }
    }

    public void HandleItemDrop(DraggableScheduleItem droppedItem)
    {
        if (!isQueueDropZone)
        {
            DestroyPlaceholder();
            return;
        }

        if (placeholder != null && placeholder.activeSelf) // Placeholder가 활성 상태일 때만 그 위치 사용
        {
            droppedItem.transform.SetParent(transform);
            droppedItem.transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());
            DestroyPlaceholder();
        }
        else // Placeholder가 없거나 비활성이면 맨 뒤에 추가 (예외 처리)
        {
            droppedItem.transform.SetParent(transform);
            droppedItem.transform.SetAsLastSibling();
            Debug.LogWarning($"[{gameObject.name}] HandleItemDrop: Placeholder가 유효하지 않아 '{droppedItem.ItemNameDebug}'을(를) 맨 뒤에 배치합니다.");
            if (placeholder != null) DestroyPlaceholder(); // 혹시 모를 비활성 placeholder 제거
        }
        RefreshLayout(false);
    }

    private void CreatePlaceholder(DraggableScheduleItem sourceItem, int siblingIndex)
    {
        if (placeholder != null) Destroy(placeholder); // 이전 placeholder 확실히 제거

        placeholder = new GameObject("Placeholder_Visible");
        placeholder.transform.SetParent(transform);

        placeholderRT = placeholder.AddComponent<RectTransform>();
        placeholderLE = placeholder.AddComponent<LayoutElement>();

        RectTransform sourceRT = sourceItem.RectTransform;
        LayoutElement sourceLE = sourceItem.GetComponent<LayoutElement>();

        float targetWidth, targetHeight;
        if (sourceLE != null && sourceLE.enabled)
        {
            targetHeight = sourceLE.preferredHeight > 0 ? sourceLE.preferredHeight : sourceRT.sizeDelta.y;
            targetWidth = sourceLE.preferredWidth > 0 ? sourceLE.preferredWidth : sourceRT.sizeDelta.x;
        }
        else
        {
            targetHeight = sourceRT.sizeDelta.y;
            targetWidth = sourceRT.sizeDelta.x;
        }

        if (fitWidthToDropZone)
        {
            RectTransform parentRect = GetComponent<RectTransform>();
            targetWidth = parentRect.rect.width - horizontalPadding;
        }

        placeholderRT.sizeDelta = new Vector2(targetWidth, targetHeight);
        placeholderLE.preferredWidth = targetWidth;
        placeholderLE.preferredHeight = targetHeight;

        placeholderRT.anchorMin = new Vector2(0.5f, 1f);
        placeholderRT.anchorMax = new Vector2(0.5f, 1f);
        placeholderRT.pivot = new Vector2(0.5f, 1f);
        placeholderRT.localScale = Vector3.one;

        Image phImage = placeholder.AddComponent<Image>();
        phImage.color = new Color(0.5f, 0.5f, 1f, 0.4f);
        phImage.raycastTarget = false;

        placeholder.transform.SetSiblingIndex(siblingIndex);
        placeholder.SetActive(true); // 생성 후 바로 활성화
    }

    private void DestroyPlaceholder()
    {
        if (placeholder != null)
        {
            KillActiveItemTweens(); // 관련된 다른 아이템 애니메이션 중지
            placeholder.SetActive(false); // Destroy 전에 비활성화하여 레이아웃 계산에서 즉시 제외
            Destroy(placeholder.gameObject); // GameObject를 파괴
            placeholder = null; // 참조를 null로 설정
            placeholderRT = null;
            placeholderLE = null;
        }
    }

    private int CalculatePlaceholderSiblingIndex(Vector2 referencePos)
    {
        int newIndex = transform.childCount;
        // placeholder가 이미 이 드롭존의 자식이고 활성 상태라면, childCount에서 1을 빼야 함
        if (placeholder != null && placeholder.transform.parent == transform && placeholder.activeSelf)
        {
            newIndex = transform.childCount - 1;
        }
        else if (placeholder != null && placeholder.transform.parent == transform && !placeholder.activeSelf)
        {
            // placeholder가 자식이지만 비활성 상태라면, 실제 활성 아이템 수 기준으로 계산해야 함
            // 또는, 이 함수는 placeholder가 활성일 때만 호출된다고 가정할 수 있음
            // 여기서는 일단 활성 placeholder 기준
        }


        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            // placeholder는 계산에서 제외 (자기 자신과 비교 방지)
            // 또한, 비활성 자식은 레이아웃에 영향을 주지 않으므로 건너뜀
            if (!child.gameObject.activeSelf || (placeholder != null && child == placeholder.transform)) continue;

            RectTransform childRect = child.GetComponent<RectTransform>();
            if (childRect == null) continue;

            Vector3[] childCorners = new Vector3[4];
            childRect.GetWorldCorners(childCorners);
            float childCenterY_World = (childCorners[1].y + childCorners[0].y) / 2f;

            if (referencePos.y > childCenterY_World)
            {
                newIndex = i;
                // 만약 placeholder가 newIndex보다 뒤에 있었다면, newIndex는 그대로 유지
                // 만약 placeholder가 newIndex보다 앞에 있었다면, newIndex는 i가 되어야 함
                // 현재 로직은 placeholder를 제외하고 순수한 아이템들 사이의 인덱스를 찾는 것에 가까움
                // placeholder가 들어갈 '빈자리'의 인덱스를 찾는 것
                if (placeholder != null && placeholder.transform.parent == transform && placeholder.activeSelf && i > placeholder.transform.GetSiblingIndex())
                {
                    // 이 경우는 placeholder가 i번째 아이템보다 위에 있었는데,
                    // 드래그 아이템이 i번째 아이템보다 더 위에 있다는 의미 -> placeholder는 i 위로 가야 함
                }
                break;
            }
        }
        return newIndex;
    }

    public void RefreshLayout(bool animate,bool isUseTween = false)
    {
        KillActiveItemTweens();
        // 활성화된 자식만 카운트하여 실제 아이템이 없을 때 불필요한 계산 방지
        int activeChildCount = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf) activeChildCount++;
        }
        if (activeChildCount == 0) return;

        float currentYPosition = -topPadding;
        RectTransform parentRect = GetComponent<RectTransform>();

        List<ItemLayoutState> initialStates = new List<ItemLayoutState>();
        if (animate)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                RectTransform childRT_anim = transform.GetChild(i) as RectTransform;
                if (childRT_anim != null && childRT_anim.gameObject.activeSelf)
                {
                    initialStates.Add(new ItemLayoutState(childRT_anim, childRT_anim.anchoredPosition));
                }
            }
        }

        // SiblingIndex 순서대로 순회하며 '활성화된' 자식들의 위치 계산 및 적용
        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform childRT = transform.GetChild(i) as RectTransform;
            // 비활성화된 자식 (예: 방금 DestroyPlaceholder에서 SetActive(false)된 placeholder)은 레이아웃 계산 및 애니메이션에서 제외
            if (childRT == null || !childRT.gameObject.activeSelf) continue;

            float itemHeight = 0;
            float itemWidth = 0;
            LayoutElement childLE = childRT.GetComponent<LayoutElement>();

            if (childLE != null && childLE.enabled)
            {
                itemHeight = childLE.preferredHeight > 0 ? childLE.preferredHeight : childRT.sizeDelta.y;
                itemWidth = childLE.preferredWidth > 0 ? childLE.preferredWidth : childRT.sizeDelta.x;
            }
            else
            {
                itemHeight = childRT.sizeDelta.y;
                itemWidth = childRT.sizeDelta.x;
            }

            Vector2 newSize = childRT.sizeDelta;
            if (fitWidthToDropZone)
            {
                newSize.x = parentRect.rect.width - horizontalPadding;
            }
            else
            {
                newSize.x = itemWidth;
            }
            newSize.y = itemHeight;
            childRT.sizeDelta = newSize;

            childRT.anchorMin = new Vector2(0.5f, 1f);
            childRT.anchorMax = new Vector2(0.5f, 1f);
            childRT.pivot = new Vector2(0.5f, 1f);

            Vector2 targetAnchoredPos = new Vector2(0, currentYPosition);

            // Placeholder 자체는 애니메이션하지 않고 즉시 위치로 이동
            if (placeholder != null && childRT.gameObject == placeholder)
            {
                childRT.anchoredPosition = targetAnchoredPos;
            }
            else if (animate)
            {
                ItemLayoutState? foundState = null;
                foreach (var state in initialStates) { if (state.rt == childRT) { foundState = state; break; } }

                Vector2 startPos = foundState.HasValue ? foundState.Value.initialAnchoredPos : childRT.anchoredPosition;

                if (Vector2.Distance(startPos, targetAnchoredPos) > 0.01f)
                {
                    Tween t = null; 
                 
                    if(isUseTween)//사용된거 움직일 때
                    {
                       t = childRT.DOAnchorPos(targetAnchoredPos, siblingAnimationDuration)
                       .SetEase(Ease.OutQuad)
                       .OnKill(() => { /* 필요한 정리 작업 */ });
                    }
                    else
                    {
                        t = childRT.DOAnchorPos(targetAnchoredPos, siblingAnimationDuration)
                     .SetEase(Ease.OutQuad)
                     .OnKill(() => { /* 필요한 정리 작업 */ });
                    }

                        activeItemTweens.Add(t);
                }
                else
                {
                    childRT.anchoredPosition = targetAnchoredPos;
                }
            }
            else
            {
                childRT.anchoredPosition = targetAnchoredPos;
            }
            // 활성화된 아이템만 Y 위치 누적에 포함
            currentYPosition -= (itemHeight + itemSpacing);
        }
    }

    public void RefreshLayoutAfterItemReturn()
    {
        RefreshLayout(true);
    }

    private void KillActiveItemTweens()
    {
        foreach (Tween t in activeItemTweens)
        {
            if (t != null && t.IsActive())
            {
                t.Kill(false);
            }
        }
        activeItemTweens.Clear();
    }
}
