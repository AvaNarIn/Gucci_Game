using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GridCell[] cells;

    private ItemData[] gridState = new ItemData[9];

    public DiceHandler DiceHandler;
    public TicTacToeHandler TicTacToeHandler;
    public CardHandler CardHandler;
    public ChessHandler ChessHandler;
    public RockPaperScissorsHandler CheckerHandler;
    private ItemHandler[] Handlers = new ItemHandler[5];

    private void Awake()
    {
        DiceHandler = GetComponent<DiceHandler>();
        TicTacToeHandler = GetComponent<TicTacToeHandler>();
        CardHandler = GetComponent<CardHandler>();
        ChessHandler = GetComponent<ChessHandler>();
        CheckerHandler = GetComponent<RockPaperScissorsHandler>();


        Handlers[0] = DiceHandler;
        Handlers[1] = TicTacToeHandler;
        Handlers[2] = CardHandler;
        Handlers[3] = ChessHandler;
        Handlers[4] = CheckerHandler;
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

    public void StartCounting()
    {
        foreach (ItemHandler handler in Handlers)
        {
            handler.ApplyingEffects();
        }
        foreach (ItemHandler handler in Handlers)
        {
            handler.CountingScore();
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