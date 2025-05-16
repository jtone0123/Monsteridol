using UnityEngine;
using UnityEngine.EventSystems; // Unity�� �̺�Ʈ �ý��� ����� ���� �ʼ�!

public class ScheduleDropZone : MonoBehaviour, IDropHandler // �������� ���� �� �ְ� IDropHandler �������̽� ����
{
    // (���� ����) �� ������� ������ �����ϱ� ���� ���� (��: ��� ���� ���, ���õ� ť ��)
    // public enum ZoneType { AvailableList, SelectedQueue }
    // public ZoneType zoneType;

    public void OnDrop(PointerEventData eventData)
    {
        // eventData.pointerDrag�� ���� �巡�׵ǰ� �ִ� ���� ������Ʈ�Դϴ�.
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null)
        {
            Debug.LogWarning($"{gameObject.name}: OnDrop �̺�Ʈ�� �߻�������, ��ӵ� ������Ʈ(eventData.pointerDrag)�� null�Դϴ�.");
            return;
        }

        DraggableScheduleItem draggableItem = droppedObject.GetComponent<DraggableScheduleItem>();
        if (draggableItem != null)
        {
            // ��ӵ� �������� �θ� ���� �� �����(�� ��ũ��Ʈ�� �پ��ִ� GameObject)���� �����մϴ�.
            // �̷��� �ϸ� �������� �� �г��� �ڽ����� �̵��ϰ� �˴ϴ�.
            draggableItem.transform.SetParent(transform);

            // LayoutGroup�� �� �гο� ����Ǿ� �ִٸ�, �������� �ڵ����� ���ĵ˴ϴ�.
            // RectTransform�� ��ġ�� �������� ������ �ʿ�� ���� �����ϴ�.
            // draggableItem.transform.localPosition = Vector3.zero; // �ʿ��ϴٸ� (LayoutGroup�� ���� ��)

            Debug.Log($"'{draggableItem.itemNameForDebug}' ({draggableItem.gameObject.name}) �������� '{gameObject.name}' ������� ��ӵǾ����ϴ�.");
        }
        else
        {
            Debug.LogWarning($"'{droppedObject.name}' �������� ��ӵǾ�����, DraggableScheduleItem ������Ʈ�� ã�� �� �����ϴ�.");
        }
    }
}