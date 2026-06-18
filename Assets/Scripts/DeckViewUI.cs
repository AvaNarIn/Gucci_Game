using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DeckViewUI : MonoBehaviour
{
    [Header("Панель")]
    public GameObject panel;

    [Header("UI элементы")]
    public GameObject cardPrefab;
    public Transform cardsContainer;
    public Button closeButton;

    [Header("Данные")]
    public DeckManager playerDeckManager;
    public GridManager playerGridManager;

    private Action<ItemData> onCardSelected;
    private bool replaceMode;
    private List<GameObject> spawnedCards = new List<GameObject>();

    void Start()
    {
        closeButton.onClick.AddListener(OnCloseClicked);
        panel.SetActive(false);
    }

    void OnCloseClicked()
    {
        if (replaceMode)
            panel.SetActive(false);
        else
            Close();
    }

    public void Show()
    {
        replaceMode = false;
        onCardSelected = null;
        panel.SetActive(true);
        Refresh();
    }

    public void ShowReplaceMode(Action<ItemData> callback)
    {
        replaceMode = true;
        onCardSelected = callback;
        panel.SetActive(true);
        Refresh();
    }

    public void ClosePanel()
    {
        panel.SetActive(false);
    }

    public void Refresh()
    {
        // Удаляем старые карточки
        foreach (var go in spawnedCards)
        {
            if (go != null) Destroy(go);
        }
        spawnedCards.Clear();

        // Получаем текущую колоду (оставшиеся карты) и считаем количество каждой
        List<ItemData> drawPile = playerDeckManager.GetDrawPile();
        Dictionary<ItemData, int> drawCounts = new Dictionary<ItemData, int>();
        foreach (var card in drawPile)
        {
            if (drawCounts.ContainsKey(card))
                drawCounts[card]++;
            else
                drawCounts[card] = 1;
        }

        Debug.Log($"[DeckView] Refresh: всего в колоде {drawPile.Count} карт, уникальных {drawCounts.Keys.Count}");

        // Группируем инвентарь игрока по типам
        var groupedInventory = PlayerInventory.cards.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());

        // Показываем каждый уникальный тип
        foreach (var kv in groupedInventory)
        {
            ItemData cardType = kv.Key;
            int totalOwned = kv.Value; // сколько всего таких карт в инвентаре
            int available = drawCounts.ContainsKey(cardType) ? drawCounts[cardType] : 0;

            GameObject cardGO = Instantiate(cardPrefab, cardsContainer);
            spawnedCards.Add(cardGO);

            // Находим текст и картинку, проверяя на null
            Text nameText = cardGO.GetComponentInChildren<Text>();
            if (nameText != null)
                nameText.text = $"{available}/{totalOwned}";

            Image iconImage = cardGO.GetComponent<Image>();
            if (iconImage != null && cardType.icon != null)
                iconImage.sprite = cardType.icon;

            bool isAvailable = available > 0;

            CanvasGroup cg = cardGO.GetComponent<CanvasGroup>();
            if (cg == null) cg = cardGO.AddComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = isAvailable ? 1f : 0.4f;
                cg.interactable = replaceMode && isAvailable;
            }

            if (replaceMode && isAvailable)
            {
                Button btn = cardGO.GetComponent<Button>();
                if (btn == null) btn = cardGO.AddComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    ItemData capturedCard = cardType;
                    btn.onClick.AddListener(() =>
                    {
                        onCardSelected?.Invoke(capturedCard);
                    });
                }
            }
        }
    }

    void Close()
    {
        panel.SetActive(false);
    }
}