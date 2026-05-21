using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum CellType
{
    Empty,
    Universal,
    Dice,
    Card,
    Chess,
    RockPaperScissors,
    TicTacToe
}

public class GridCell : MonoBehaviour, IDropHandler
{
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private Image healthBar;
    [SerializeField] private Text healthText;
    [SerializeField] private CellType cellType = CellType.Empty;
    [SerializeField] private float multiplier = 1f;

    public int CellIndex { get; private set; }
    public GridManager OwnerGridManager { get; private set; }
    [HideInInspector] public Draggable currentItem;

    public System.Action<int, Draggable> OnItemPlaced;
    public System.Action<int> OnItemRemoved;

    private int currentHealth;
    public int CurrentHealth => currentHealth;

    public void Init(int index, GridManager owner)
    {
        CellIndex = index;
        OwnerGridManager = owner;
        currentHealth = maxHealth;
        healthText.text = currentHealth.ToString();
        healthText.color = Color.green;
        UpdateHealthBar();
    }

    public void SetProperties(int newMaxHealth, CellType newType, float newMultiplier)
    {
        maxHealth = newMaxHealth;
        cellType = newType;
        multiplier = newMultiplier;
        currentHealth = newMaxHealth;
        healthText.text = currentHealth.ToString();
        healthText.color = Color.green;
        UpdateHealthBar();
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        healthText.text = currentHealth.ToString();
        healthText.color = Color.green;
        UpdateHealthBar();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (currentItem != null) return;

        Draggable draggable = eventData.pointerDrag?.GetComponent<Draggable>();
        if (draggable == null) return;
        if (draggable.OwnerGridManager != OwnerGridManager) return;

        if (draggable.OriginalCell != null && draggable.OriginalCell != this)
            return;

        if (draggable.OriginalCell == this)
            return;

        if (TurnManager.Instance.IsPlayerGridManager(draggable.OwnerGridManager))
        {
            if (!TurnManager.Instance.CanAffordPlayer(draggable.ItemData.score))
                return;
        }

        PlaceItem(draggable);
    }

    public void PlaceItem(Draggable item)
    {
        if (currentItem != null) return;

        currentHealth = maxHealth;
        healthText.text = currentHealth.ToString();
        healthText.color = Color.white;
        currentItem = item;
        item.AttachToCell(transform);
        item.SetCellIndex(CellIndex);

        if (TurnManager.Instance.IsPlayerGridManager(item.OwnerGridManager))
            TurnManager.Instance.SpendPlayerMana(item.ItemData.score);
        else if (TurnManager.Instance.IsBotGridManager(item.OwnerGridManager))
            TurnManager.Instance.SpendBotMana(item.ItemData.score);

        OnItemPlaced?.Invoke(CellIndex, item);
        UpdateHealthBar();
    }

    public void RemoveItem()
    {
        Destroy(currentItem.gameObject);
        currentItem = null;
        OnItemRemoved?.Invoke(CellIndex);
        UpdateHealthBar();
        healthText.color = Color.green;
    }

    public void TakeDamage(int damage)
    {
        if (currentItem == null) return;
        if (damage <= 0) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            RemoveItem();
            currentHealth = maxHealth;
        }
        healthText.text = currentHealth.ToString();
        UpdateHealthBar();
    }

    public float GetMultiplier(ItemData item)
    {
        if (cellType == CellType.Empty) return 1f;
        if (cellType == CellType.Universal) return multiplier;

        switch (cellType)
        {
            case CellType.Dice:
                return item is DiceData ? multiplier : 1f;
            case CellType.Card:
                return item is CardData ? multiplier : 1f;
            case CellType.Chess:
                return item is ChessData ? multiplier : 1f;
            case CellType.RockPaperScissors:
                return item is RockPaperScissorsData ? multiplier : 1f;
            case CellType.TicTacToe:
                return item is TicTacToeData ? multiplier : 1f;
            default:
                return 1f;
        }
    }

    private void UpdateHealthBar()
    {
        healthBar.fillAmount = (float)currentHealth / maxHealth;
    }
}