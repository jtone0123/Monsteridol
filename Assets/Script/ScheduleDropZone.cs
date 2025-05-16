using UnityEngine;
using UnityEngine.EventSystems; // Unity의 이벤트 시스템 사용을 위해 필수!

public class ScheduleDropZone : MonoBehaviour, IDropHandler // 아이템을 받을 수 있게 IDropHandler 인터페이스 구현
{
    // (선택 사항) 이 드롭존의 유형을 구분하기 위한 변수 (예: 사용 가능 목록, 선택된 큐 등)
    // public enum ZoneType { AvailableList, SelectedQueue }
    // public ZoneType zoneType;

    public void OnDrop(PointerEventData eventData)
    {
        // eventData.pointerDrag는 현재 드래그되고 있던 게임 오브젝트입니다.
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null)
        {
            Debug.LogWarning($"{gameObject.name}: OnDrop 이벤트는 발생했으나, 드롭된 오브젝트(eventData.pointerDrag)가 null입니다.");
            return;
        }

        DraggableScheduleItem draggableItem = droppedObject.GetComponent<DraggableScheduleItem>();
        if (draggableItem != null)
        {
            // 드롭된 아이템의 부모를 현재 이 드롭존(이 스크립트가 붙어있는 GameObject)으로 설정합니다.
            // 이렇게 하면 아이템이 이 패널의 자식으로 이동하게 됩니다.
            draggableItem.transform.SetParent(transform);

            // LayoutGroup이 이 패널에 적용되어 있다면, 아이템은 자동으로 정렬됩니다.
            // RectTransform의 위치를 수동으로 설정할 필요는 보통 없습니다.
            // draggableItem.transform.localPosition = Vector3.zero; // 필요하다면 (LayoutGroup이 없을 때)

            Debug.Log($"'{draggableItem.itemNameForDebug}' ({draggableItem.gameObject.name}) 아이템이 '{gameObject.name}' 드롭존에 드롭되었습니다.");
        }
        else
        {
            Debug.LogWarning($"'{droppedObject.name}' 아이템이 드롭되었지만, DraggableScheduleItem 컴포넌트를 찾을 수 없습니다.");
        }
    }
}