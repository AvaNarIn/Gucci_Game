using System;
using UnityEngine;
using UnityEngine.UI;

public class ItemRewardUI : MonoBehaviour
{
    public Button[] choiceButtons;
    public Button skipButton;

    private ItemData[] offeredItems;
    private Action onComplete;
    private DeckViewUI deckView;
    private DeckManager playerDeckManager;

    void Start()
    {
        skipButton.onClick.AddListener(() => {
            GiveRandomBuffAndClose();
        });
    }

    public void Init(DeckViewUI deckViewUI, DeckManager deckManager)
    {
        deckView = deckViewUI;
        playerDeckManager = deckManager;
    }

    public void Offer(ItemSet set1, ItemSet set2, ItemDatabase database, Action onFinished)
    {
        onComplete = onFinished;
        gameObject.SetActive(true);
        offeredItems = new ItemData[3];
        offeredItems[0] = database.GetRandomItem(set1);
        offeredItems[1] = database.GetRandomItem(set2);
        offeredItems[2] = database.GetRandomItemExcluding(set1, set2);
        if (offeredItems[2] == null)
            offeredItems[2] = offeredItems[0] ?? offeredItems[1];

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            if (offeredItems[i] != null)
            {
                // Устанавливаем текст
                Text buttonText = choiceButtons[i].GetComponentInChildren<Text>();
                if (buttonText != null)
                    buttonText.text = offeredItems[i].displayName + " (" + offeredItems[i].score + ")";

                // Устанавливаем иконку
                Image buttonImage = choiceButtons[i].GetComponent<Image>();
                if (buttonImage != null && offeredItems[i].icon != null)
                    buttonImage.sprite = offeredItems[i].icon;
                else if (buttonImage == null)
                {
                    // Если Image на самой кнопке нет, ищем в дочерних (на случай, если иконка отдельным объектом)
                    Image childImage = choiceButtons[i].GetComponentInChildren<Image>();
                    if (childImage != null && offeredItems[i].icon != null)
                        childImage.sprite = offeredItems[i].icon;
                }

                choiceButtons[i].interactable = true;
            }
            else
            {
                Text buttonText = choiceButtons[i].GetComponentInChildren<Text>();
                if (buttonText != null)
                    buttonText.text = "Нет доступных";
                choiceButtons[i].interactable = false;
            }
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => OnItemChosen(index));
        }
    }

    void OnItemChosen(int idx)
    {
        if (offeredItems[idx] == null) return;

        if (PlayerInventory.cards.Count < 45)
        {
            PlayerInventory.AddCard(offeredItems[idx]);
            playerDeckManager.AddCardToDrawPile(offeredItems[idx]);
            deckView.Refresh();
            MetaGameManager.Instance?.RefreshDeckButtonText();
            Close();
        }
        else
        {
            deckView.ShowReplaceMode((selectedCardType) =>
            {
                PlayerInventory.RemoveCard(selectedCardType);
                playerDeckManager.RemoveCardFromDrawPile(selectedCardType);
                PlayerInventory.AddCard(offeredItems[idx]);
                playerDeckManager.AddCardToDrawPile(offeredItems[idx]);
                deckView.Refresh();
                MetaGameManager.Instance?.RefreshDeckButtonText();
                deckView.ClosePanel();
                Close();
            });
        }
    }

    void GiveRandomBuffAndClose()
    {
        TemporaryBuffDatabase buffDB = MetaGameManager.Instance.buffDatabase;
        if (buffDB != null && buffDB.allBuffs.Count > 0)
        {
            TemporaryBuffData randomBuff = buffDB.GetRandomBuff();
            if (randomBuff != null)
            {
                int duration = UnityEngine.Random.Range(1, 6);
                PlayerInventory.activeBuffs.Add(new BuffInstance(randomBuff, duration));
                if (TurnManager.Instance != null)
                    TurnManager.Instance.UpdateBuffDisplay();
            }
        }
        Close();
    }

    void Close()
    {
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }
}