using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilitySlotsUI : MonoBehaviour
{
    [Header("Слоты")]
    public Image[] slotImages;          // 6 Image для иконок способностей
    public Sprite emptySlotSprite;

    [Header("Панель просмотра")]
    public GameObject detailPanel;
    public Text detailNameText;
    public Text detailSetText;
    public Text detailDescriptionText;

    [Header("Данные")]
    public AbilityDatabase abilityDatabase;

    private Action<AbilityData> onReplaceCallback;
    private bool replaceMode;
    private int currentlyViewedIndex = -1;

    void Start()
    {
        detailPanel.SetActive(false);
        UpdateSlots();
    }

    public void UpdateSlots()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            if (i < PlayerInventory.abilities.Count)
                slotImages[i].sprite = PlayerInventory.abilities[i].icon;
            else
                slotImages[i].sprite = emptySlotSprite;

            Button btn = slotImages[i].GetComponent<Button>();
            if (btn == null)
                btn = slotImages[i].gameObject.AddComponent<Button>();

            btn.onClick.RemoveAllListeners();
            int index = i;

            if (replaceMode && index < PlayerInventory.abilities.Count)
            {
                btn.onClick.AddListener(() => OnSlotClickedForReplace(index));
            }
            else
            {
                btn.onClick.AddListener(() => OnSlotClicked(index));
            }
        }
    }

    void OnSlotClicked(int index)
    {
        if (index >= PlayerInventory.abilities.Count) return;

        if (currentlyViewedIndex == index)
        {
            CloseDetail();
            return;
        }

        AbilityData ability = PlayerInventory.abilities[index];
        detailNameText.text = ability.abilityName;
        detailSetText.text = "Тип: " + ability.set.ToString();
        detailDescriptionText.text = ability.description;
        detailPanel.SetActive(true);
        currentlyViewedIndex = index;
    }

    void CloseDetail()
    {
        detailPanel.SetActive(false);
        currentlyViewedIndex = -1;
    }

    void OnSlotClickedForReplace(int index)
    {
        if (replaceMode && index < PlayerInventory.abilities.Count && onReplaceCallback != null)
        {
            onReplaceCallback?.Invoke(PlayerInventory.abilities[index]);
            replaceMode = false;
            onReplaceCallback = null;
            CloseDetail();
            UpdateSlots();
        }
    }

    public void StartReplaceMode(Action<AbilityData> callback)
    {
        replaceMode = true;
        onReplaceCallback = callback;
        CloseDetail();
        UpdateSlots();
    }

    public void CancelReplaceMode()
    {
        replaceMode = false;
        onReplaceCallback = null;
        UpdateSlots();
    }
}