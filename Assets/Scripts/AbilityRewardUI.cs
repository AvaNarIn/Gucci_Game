using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilityRewardUI : MonoBehaviour
{
    public Button[] choiceButtons;
    public Button skipButton;

    private AbilityData[] offeredAbilities;
    private Action onComplete;
    private AbilitySlotsUI abilitySlotsUI;

    void Start()
    {
        skipButton.onClick.AddListener(() => {
            GiveRandomBuffAndClose();
        });
    }

    public void Init(AbilitySlotsUI slotsUI)
    {
        abilitySlotsUI = slotsUI;
    }

    public void Offer(ItemSet set1, ItemSet set2, AbilityDatabase db, Action onFinished)
    {
        onComplete = onFinished;
        gameObject.SetActive(true);
        offeredAbilities = new AbilityData[3];
        offeredAbilities[0] = db.GetRandomAbility(set1, PlayerInventory.abilities);
        offeredAbilities[1] = db.GetRandomAbility(set2, PlayerInventory.abilities);
        offeredAbilities[2] = db.GetRandomAbility(set1, PlayerInventory.abilities)
                           ?? db.GetRandomAbility(set2, PlayerInventory.abilities);

        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            if (offeredAbilities[i] != null)
            {
                // Текст
                Text buttonText = choiceButtons[i].GetComponentInChildren<Text>();
                if (buttonText != null)
                    buttonText.text = offeredAbilities[i].abilityName;

                // Иконка
                Image buttonImage = choiceButtons[i].GetComponent<Image>();
                if (buttonImage != null && offeredAbilities[i].icon != null)
                    buttonImage.sprite = offeredAbilities[i].icon;
                else if (buttonImage == null)
                {
                    Image childImage = choiceButtons[i].GetComponentInChildren<Image>();
                    if (childImage != null && offeredAbilities[i].icon != null)
                        childImage.sprite = offeredAbilities[i].icon;
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
            choiceButtons[i].onClick.AddListener(() => OnAbilityChosen(idx));
        }
    }

    void OnAbilityChosen(int idx)
    {
        if (offeredAbilities[idx] == null) return;

        if (PlayerInventory.abilities.Count < PlayerInventory.maxAbilities)
        {
            PlayerInventory.AddAbility(offeredAbilities[idx]);
            abilitySlotsUI.UpdateSlots();
            Close();
        }
        else
        {
            abilitySlotsUI.StartReplaceMode((oldAbility) =>
            {
                PlayerInventory.ReplaceAbility(oldAbility, offeredAbilities[idx]);
                abilitySlotsUI.CancelReplaceMode();
                abilitySlotsUI.UpdateSlots();
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