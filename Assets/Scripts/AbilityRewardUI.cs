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
    private int pendingReplaceIndex = -1;
    private bool isReplacing = false;   // активен ли режим замены

    void Start()
    {
        skipButton.onClick.AddListener(() => { GiveRandomBuffAndClose(); });
    }

    public void Init(AbilitySlotsUI slotsUI)
    {
        abilitySlotsUI = slotsUI;
    }

    public void Offer(ItemSet set1, ItemSet set2, AbilityDatabase db, Action onFinished)
    {
        onComplete = onFinished;
        gameObject.SetActive(true);
        pendingReplaceIndex = -1;
        isReplacing = false;
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
                choiceButtons[i].GetComponentInChildren<Text>().text = offeredAbilities[i].abilityName;
                choiceButtons[i].interactable = true;
                SetButtonColor(choiceButtons[i], Color.white);
            }
            else
            {
                choiceButtons[i].GetComponentInChildren<Text>().text = "Нет доступных";
                choiceButtons[i].interactable = false;
                SetButtonColor(choiceButtons[i], Color.gray);
            }
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => OnAbilityChosen(idx));
        }
    }

    void SetButtonColor(Button btn, Color color)
    {
        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    void OnAbilityChosen(int idx)
    {
        if (offeredAbilities[idx] == null) return;

        if (PlayerInventory.abilities.Count < PlayerInventory.maxAbilities)
        {
            // Есть свободный слот – просто добавляем
            PlayerInventory.AddAbility(offeredAbilities[idx]);
            abilitySlotsUI.UpdateSlots();
            Close();
        }
        else
        {
            // Замена: подсвечиваем выбранную способность зелёным
            pendingReplaceIndex = idx;
            isReplacing = true;
            for (int i = 0; i < 3; i++)
            {
                if (i == idx)
                    SetButtonColor(choiceButtons[i], Color.green);
                else
                    SetButtonColor(choiceButtons[i], Color.white);
            }

            // Включаем режим замены на слотах (панель награды не скрываем)
            abilitySlotsUI.StartReplaceMode(OnReplaceConfirmed);
        }
    }

    void OnReplaceConfirmed(AbilityData oldAbility)
    {
        // Замена произошла – завершаем награду
        if (pendingReplaceIndex >= 0 && offeredAbilities[pendingReplaceIndex] != null)
        {
            PlayerInventory.ReplaceAbility(oldAbility, offeredAbilities[pendingReplaceIndex]);
            abilitySlotsUI.CancelReplaceMode();
            abilitySlotsUI.UpdateSlots();
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
        if (isReplacing)
        {
            abilitySlotsUI.CancelReplaceMode();
            isReplacing = false;
        }
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }
}