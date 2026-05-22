using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    public Transform handPanel;
    [SerializeField] private GameObject draggablePrefab;
    [SerializeField] private List<ItemData> deck;
    private List<ItemData> drawPile;
    private int maxHandSize = 6;

    private void Awake()
    {
        drawPile = new List<ItemData>(deck);
        Shuffle(drawPile);
    }

    public void SetCustomDeck(List<ItemData> newDeck)
    {
        drawPile = new List<ItemData>(newDeck);
        Shuffle(drawPile);
    }

    public void DrawInitialHand()
    {
        int toDraw = Mathf.Min(3, drawPile.Count, maxHandSize);
        DrawCards(toDraw);
    }

        public void DrawTurnCards(int count)
        {
        for (int i = 0; i < count; i++)
        {
            if (drawPile.Count == 0) break;
            ItemData data = drawPile[0];
            drawPile.RemoveAt(0);

            if (data == null)
            {
                Debug.LogError("[DeckManager] В drawPile оказался null! Проверьте формирование колоды.");
                continue;
            }

            Debug.Log($"[DeckManager] Создаю предмет в руке: {data.displayName} (score: {data.score})");
            gridManager.CreateItemInHand(data, handPanel, false);
        }
    }

    private void DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (drawPile.Count == 0) break;
            ItemData data = drawPile[0];
            drawPile.RemoveAt(0);
            gridManager.CreateItemInHand(data, handPanel, false);
        }
    }

    private void Shuffle(List<ItemData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            ItemData temp = list[i];
            int rand = Random.Range(i, list.Count);
            list[i] = list[rand];
            list[rand] = temp;
        }
    }

    public List<ItemData> GetDeck() => deck;
    public List<ItemData> GetDrawPile() => drawPile;
    public bool IsHandEmpty => handPanel.childCount == 0;
    public bool IsDeckEmpty => drawPile.Count == 0;

    public void RemoveCardFromDrawPile(ItemData card)
    {
        drawPile.Remove(card);
    }

    public void AddCardToDrawPile(ItemData card)
    {
        drawPile.Add(card);
    }
}