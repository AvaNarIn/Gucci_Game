using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GridCell[] cells;
    [SerializeField] private GameObject draggablePrefab;
    private ItemData[] gridState = new ItemData[9];

    private ItemHandler[] handlers;

    private void Awake()
    {
        handlers = GetComponents<ItemHandler>();
    }

    private void Start()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Init(i, this);
            cells[i].OnItemPlaced += OnItemPlacedInCell;
            cells[i].OnItemRemoved += OnItemRemovedFromCell;
        }
    }

    public IEnumerator CountingCoroutine(System.Action<int> onComplete)
    {
        foreach (var handler in handlers)
        {
            yield return handler.ApplyingEffects_Coroutine();
        }

        foreach (var handler in handlers)
        {
            yield return handler.CountingScore_Coroutine();
        }

        int total = Mathf.RoundToInt(handlers.Sum(h => h != null ? h.LastScore : 0f));
        onComplete?.Invoke(total);
    }

    public bool PlaceExistingDraggable(Draggable draggable, int cellIndex)
    {
        if (cellIndex < 0 || cellIndex >= 9) return false;
        if (cells[cellIndex].currentItem != null) return false;
        if (draggable.OwnerGridManager != this) return false;

        cells[cellIndex].PlaceItem(draggable);
        return true;
    }

    public Draggable CreateItemInHand(ItemData data, Transform handParent)
    {
        GameObject itemObject = Instantiate(draggablePrefab, handParent);
        Draggable draggable = itemObject.GetComponent<Draggable>();
        draggable.Initialize(this);
        draggable.SetItemData(data);
        return draggable;
    }

    private void OnItemPlacedInCell(int index, Draggable item)
    {
        gridState[index] = item.ItemData;
    }

    private void OnItemRemovedFromCell(int index)
    {
        gridState[index] = null;
    }

    public ItemData[] GetGridState() => gridState;
    public GridCell[] GetCells() => cells;

    public List<int> GetEmptyCells()
    {
        List<int> empty = new List<int>();
        for (int i = 0; i < cells.Length; i++)
            if (cells[i].currentItem == null)
                empty.Add(i);
        return empty;
    }
}