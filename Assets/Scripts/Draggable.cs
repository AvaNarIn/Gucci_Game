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

    [Header("Отображение стоимости")]
    [SerializeField] private Text costText;
    public Text CostText => costText;

    [Header("Анимация")]
    [SerializeField] private float placePopScale = 1.18f;
    [SerializeField] private float placePopDuration = 0.22f;
    [SerializeField] private float pickupScale = 1.1f;
    [SerializeField] private float pickupDuration = 0.12f;
    [SerializeField] private float returnWiggleDuration = 0.3f;
    [SerializeField] private float returnWiggleStrength = 10f;
    [SerializeField] private float rollDuration = 0.7f;

    private string originalText;
    private Color originalColor;

    private Vector3 defaultScale = Vector3.one;
    private Quaternion defaultRotation = Quaternion.identity;

    private Coroutine textCoroutine;
    private Coroutine motionCoroutine;
    private Coroutine textPopCoroutine;

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

        defaultScale = rectTransform.localScale;
        defaultRotation = rectTransform.localRotation;

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

    /// <summary>Временный текст (урон, очки, бросок) с автоматическим восстановлением.</summary>
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

    public void ShowDamageTemporarily(int damage)
    {
        ShowTextTemporarily($"-{damage}", Color.red, 0.5f);
        PlayTextPop();
    }

    public void ShowScoreGain(int amount)
    {
        ShowTextTemporarily($"+{amount}", Color.green, 0.5f);
        PlayTextPop();
    }

    // ===================== Анимация =====================

    /// <summary>Эффект "подпрыгивания" при установке предмета на клетку.</summary>
    public void PlayPlaceAnimation()
    {
        if (motionCoroutine != null) StopCoroutine(motionCoroutine);
        motionCoroutine = StartCoroutine(ScalePop(placePopScale, placePopDuration));
    }

    /// <summary>Лёгкое увеличение при взятии предмета в руку.</summary>
    public void PlayPickupAnimation()
    {
        if (motionCoroutine != null) StopCoroutine(motionCoroutine);
        motionCoroutine = StartCoroutine(ScaleTo(defaultScale * pickupScale, pickupDuration));
    }

    /// <summary>Покачивание, когда предмет вернулся в исходную позицию (недопустимый ход).</summary>
    public void PlayReturnAnimation()
    {
        if (motionCoroutine != null) StopCoroutine(motionCoroutine);
        motionCoroutine = StartCoroutine(WiggleAndReset());
    }

    private IEnumerator ScalePop(float peak, float duration)
    {
        // 0 -> peak -> 1 по масштабу (эффект "пружины")
        Vector3 target = defaultScale * peak;
        float half = duration * 0.4f;
        yield return ScaleLerp(rectTransform.localScale, target, half);
        yield return ScaleLerp(target, defaultScale, duration - half);
        rectTransform.localScale = defaultScale;
        motionCoroutine = null;
    }

    private IEnumerator ScaleTo(Vector3 target, float duration)
    {
        yield return ScaleLerp(rectTransform.localScale, target, duration);
        motionCoroutine = null;
    }

    private IEnumerator ScaleLerp(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        if (duration <= 0f) { rectTransform.localScale = to; yield break; }
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            rectTransform.localScale = Vector3.LerpUnclamped(from, to, p);
            yield return null;
        }
        rectTransform.localScale = to;
    }

    private IEnumerator WiggleAndReset()
    {
        rectTransform.localScale = defaultScale;
        rectTransform.localRotation = defaultRotation;
        Vector2 basePos = rectTransform.anchoredPosition;
        float t = 0f;
        while (t < returnWiggleDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / returnWiggleDuration);
            float offset = Mathf.Sin(p * Mathf.PI * 6f) * (1f - p) * returnWiggleStrength;
            rectTransform.anchoredPosition = basePos + new Vector2(offset, 0f);
            yield return null;
        }
        rectTransform.anchoredPosition = basePos;
        motionCoroutine = null;
    }

    private void PlayTextPop()
    {
        if (costText == null) return;
        if (textPopCoroutine != null) StopCoroutine(textPopCoroutine);
        textPopCoroutine = StartCoroutine(TextPopRoutine());
    }

    private IEnumerator TextPopRoutine()
    {
        RectTransform rt = costText.rectTransform;
        Vector3 baseScale = Vector3.one;
        float dur = 0.25f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            // плавный масштаб 1 -> 1.5 -> 1
            float s = 1f + Mathf.Sin(p * Mathf.PI) * 0.5f;
            rt.localScale = baseScale * s;
            yield return null;
        }
        rt.localScale = baseScale;
        textPopCoroutine = null;
    }

    /// <summary>Анимация броска кубика: прокрутка случайных значений и остановка на финальном.</summary>
    public IEnumerator PlayRollAnimation(int faces, int finalValue)
    {
        if (costText == null)
        {
            yield return new WaitForSeconds(rollDuration);
            ShowRollValue(finalValue);
            yield break;
        }

        if (textCoroutine != null) { StopCoroutine(textCoroutine); textCoroutine = null; }

        costText.color = Color.yellow;

        float t = 0f;
        float interval = 0.04f;
        float acc = interval;
        while (t < rollDuration)
        {
            float dt = Time.deltaTime;
            t += dt;
            acc += dt;

            if (acc >= interval)
            {
                acc = 0f;
                costText.text = Random.Range(1, Mathf.Max(2, faces + 1)).ToString();
                interval += 0.012f; // замедляем прокрутку к концу
            }

            float p = Mathf.Clamp01(t / rollDuration);
            float angle = Mathf.Sin(t * 38f) * (1f - p) * 14f;
            rectTransform.localRotation = defaultRotation * Quaternion.Euler(0f, 0f, angle);
            rectTransform.localScale = defaultScale * (1f + (1f - p) * 0.1f);
            yield return null;
        }

        rectTransform.localRotation = defaultRotation;
        rectTransform.localScale = defaultScale;
        ShowRollValue(finalValue);
    }

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

        PlayPickupAnimation();
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

        // вернулись в исходную позицию (ход не удался) — проигрываем покачивание
        PlayReturnAnimation();
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
