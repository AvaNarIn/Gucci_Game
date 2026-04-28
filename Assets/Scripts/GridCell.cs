using UnityEngine;
using UnityEngine.EventSystems;

public class GridCell : MonoBehaviour, IDropHandler
{
    public int CellIndex { get; private set; }
    [HideInInspector] public Draggable currentItem;

    public System.Action<int, Draggable> OnItemPlaced;
    public System.Action<int> OnItemRemoved;

    public void Init(int index) => CellIndex = index;

    public void OnDrop(PointerEventData eventData)
    {
        if (currentItem != null) return;

        Draggable draggable = eventData.pointerDrag.GetComponent<Draggable>();

        PlaceItem(draggable);
    }

    public void PlaceItem(Draggable item)
    {
        currentItem = item;
        item.AttachToCell(transform);
        OnItemPlaced?.Invoke(CellIndex, item);
    }

    public void RemoveItem(Draggable item)
    {
        currentItem = null;
        OnItemRemoved?.Invoke(CellIndex);
    }
}