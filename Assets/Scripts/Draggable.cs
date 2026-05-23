using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] private ItemData itemData;
    public ItemData ItemData => itemData;

    [SerializeField] private bool isDraggable = true;
    public GridManager OwnerGridManager { get; private set; }

    [Header("ќтображение стоимости")]
    [SerializeField] private Text costText;
    public Text CostText => costText;

    private string originalText;
    private Color originalColor;

    private Coroutine textCoroutine;

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Transform originalParent;
    private Vector2 originalAnchoredPos;
    private GridCell originalCell;
    public GridCell OriginalCell => originalCell;

    private int currentCellIndex = -1;

    public System.Action<Draggable> OnClicked;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup.blocksRaycasts = true;

        if (costText != null)
        {
            originalText = costText.text;
            originalColor = costText.color;
        }
    }

    public void Initialize(GridManager owner) => OwnerGridManager = owner;

    public void SetItemData(ItemData data)
    {
        itemData = data;
        GetComponent<Image>().sprite = data.icon;

        if (costText != null)
        {
            originalText = data.score.ToString();
            originalColor = costText.color;
            costText.text = originalText;
            costText.color = originalColor;
        }
    }

    public void SetDraggable(bool draggable) { isDraggable = draggable; }

    /// <summary>¬ременный текст (урон, очки, бросок) с автоматическим восстановлением.</summary>
    public void ShowTextTemporarily(string text, Color color, float duration)
    {
        if (costText == null) return;

        if (textCoroutine != null)
            StopCoroutine(textCoroutine);

        costText.text = text;
        costText.color = color;

        textCoroutine = StartCoroutine(RestoreAfterDuration(duration));
    }

    private IEnumerator RestoreAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (costText != null)
        {
            costText.text = originalText;
            costText.color = originalColor;
        }
        textCoroutine = null;
    }

    public void ShowRollValue(int value) =>
        ShowTextTemporarily(value.ToString(), Color.yellow, 0.5f);

    public void ShowDamageTemporarily(int damage) =>
        ShowTextTemporarily($"-{damage}", Color.red, 0.5f);

    public void ShowScoreGain(int amount) =>
        ShowTextTemporarily($"+{amount}", Color.green, 0.5f);

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isDraggable)
        {
            eventData.pointerDrag = null;
            return;
        }

        originalParent = transform.parent;
        originalAnchoredPos = rectTransform.anchoredPosition;
        originalCell = originalParent?.GetComponent<GridCell>();

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;
        canvasGroup.blocksRaycasts = true;

        if (transform.parent == canvas.transform)
            ReturnToOriginalCell();
    }

    public void AttachToCell(Transform cellTransform)
    {
        transform.SetParent(cellTransform, false);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        GridCell cell = cellTransform.GetComponent<GridCell>();
        if (cell != null)
            SetCellIndex(cell.CellIndex);
    }

    public void ReturnToOriginalCell()
    {
        if (originalCell != null)
        {
            AttachToCell(originalCell.GetComponent<RectTransform>());
        }
        else
        {
            transform.SetParent(originalParent, false);
            rectTransform.anchoredPosition = originalAnchoredPos;
            SetCellIndex(-1);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (transform.parent == canvas?.transform) return;
        OnClicked?.Invoke(this);
    }

    public void DestroyItem()
    {
        if (OwnerGridManager != null && currentCellIndex >= 0 && currentCellIndex < 9)
        {
            OwnerGridManager.RemoveItemFromCell(currentCellIndex);
        }
        Destroy(gameObject);
    }

    public void SetCellIndex(int index)
    {
        currentCellIndex = index;
    }
}