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

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Transform originalParent;
    private Vector2 originalAnchoredPos;
    private GridCell originalCell;

    public System.Action<Draggable> OnClicked;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup.blocksRaycasts = true;
    }

    public void Initialize(GridManager owner) => OwnerGridManager = owner;

    public void SetItemData(ItemData data)
    {
        itemData = data;
        GetComponent<Image>().sprite = data.icon;
    }
    public void SetDraggable(bool draggable)
    {
        isDraggable = draggable;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        originalParent = transform.parent;
        originalAnchoredPos = rectTransform.anchoredPosition;
        originalCell = originalParent?.GetComponent<GridCell>();

        if (originalCell != null)
            originalCell.RemoveItem(this);

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
    }

    public void ReturnToOriginalCell()
    {
        if (originalCell != null)
            originalCell.PlaceItem(this);
        else
        {
            transform.SetParent(originalParent, false);
            rectTransform.anchoredPosition = originalAnchoredPos;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (transform.parent == canvas?.transform)
            return;

        OnClicked?.Invoke(this);
    }
}