using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GridCell[] cells;

    private ItemData[] gridState = new ItemData[9];

    public DiceHandler DiceHandler;
    public TicTacToeHandler TicTacToeHandler;

    private void Awake()
    {
        DiceHandler = GetComponent<DiceHandler>();
        TicTacToeHandler = GetComponent<TicTacToeHandler>();
    }

    private void Start()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Init(i);
            cells[i].OnItemPlaced += OnItemPlacedInCell;
            cells[i].OnItemRemoved += OnItemRemovedFromCell;
        }
    }

    public void StartRound()
    {
        DiceHandler.PlayRound();
        TicTacToeHandler.PlayRound();
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