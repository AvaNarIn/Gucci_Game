using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GridCell[] cells;

    private ItemData[] gridState = new ItemData[9];

    private void Start()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Init(i);
            cells[i].OnItemPlaced += OnItemPlacedInCell;
            cells[i].OnItemRemoved += OnItemRemovedFromCell;
        }
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
}