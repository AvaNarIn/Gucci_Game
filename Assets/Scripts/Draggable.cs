using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private ItemData itemData;
    public ItemData ItemData => itemData;

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Transform originalParent;
    private Vector2 originalAnchoredPos;
    private GridCell originalCell;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
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
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
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
}