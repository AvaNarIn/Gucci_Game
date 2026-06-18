using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TemporaryBuffRewardUI : MonoBehaviour
{
    public Button[] choiceButtons;
    public Button skipButton;

    private TemporaryBuffData[] offeredBuffsData;
    private int[] offeredDurations;
    private Action onComplete;

    void Start()
    {
        skipButton.onClick.AddListener(() => {
            GiveRandomBuffAndClose();
        });
    }

    public void Offer(TemporaryBuffDatabase database, Action onFinished)
    {
        onComplete = onFinished;
        gameObject.SetActive(true);

        offeredBuffsData = new TemporaryBuffData[2];
        offeredDurations = new int[2];

        for (int i = 0; i < 2; i++)
        {
            offeredBuffsData[i] = database.GetRandomBuff();
            offeredDurations[i] = UnityEngine.Random.Range(1, 6);

            int index = i;
            if (offeredBuffsData[i] != null)
            {
                string text = $"{offeredBuffsData[i].buffName} (длительность: {offeredDurations[i]})";
                choiceButtons[i].GetComponentInChildren<Text>().text = text;
                choiceButtons[i].interactable = true;
            }
            else
            {
                choiceButtons[i].GetComponentInChildren<Text>().text = "Нет доступных";
                choiceButtons[i].interactable = false;
            }
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => OnBuffChosen(index));
        }
    }

    void OnBuffChosen(int idx)
    {
        if (offeredBuffsData[idx] != null)
        {
            BuffInstance newBuff = new BuffInstance(offeredBuffsData[idx], offeredDurations[idx]);
            PlayerInventory.activeBuffs.Add(newBuff);
            if (TurnManager.Instance != null)
                TurnManager.Instance.UpdateBuffDisplay();
        }
        Close();
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