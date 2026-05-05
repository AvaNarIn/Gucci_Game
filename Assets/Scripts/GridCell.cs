using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridCell : MonoBehaviour, IDropHandler
{
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private Image healthBar;

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
        UpdateHealthBar();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (currentItem != null) return;

        Draggable draggable = eventData.pointerDrag?.GetComponent<Draggable>();
        if (draggable == null) return;

        if (draggable.OwnerGridManager != OwnerGridManager) return;

        PlaceItem(draggable);
    }

    public void PlaceItem(Draggable item)
    {
        if (currentItem != null) return;

        currentItem = item;
        item.AttachToCell(transform);
        OnItemPlaced?.Invoke(CellIndex, item);
        UpdateHealthBar();
    }

    public void RemoveItem(Draggable item)
    {
        if (currentItem == item)
        {
            currentItem = null;
            OnItemRemoved?.Invoke(CellIndex);
            UpdateHealthBar();
        }
    }

    public void TakeDamage(int damage)
    {
        if (currentItem == null) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            if (currentItem != null)
            {
                Draggable item = currentItem;
                RemoveItem(item);
                Destroy(item.gameObject);
            }
        }
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.fillAmount = (float)currentHealth / maxHealth;
    }
}