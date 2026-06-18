using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AbilitySlotsUI : MonoBehaviour
{
    [Header("Слоты")]
    public Image[] slotImages;
    public Sprite emptySlotSprite;

    [Header("Панель просмотра")]
    public GameObject detailPanel;
    public Text detailNameText;
    public Text detailSetText;
    public Text detailDescriptionText;

    [Header("Режим замены")]
    public GameObject cancelReplaceButton;
    public UnityEvent OnCancelReplace;

    [Header("Данные")]
    public AbilityDatabase abilityDatabase;

    private Action<AbilityData> onReplaceCallback;
    private bool replaceMode;
    private int currentlyViewedIndex = -1;
    private int selectedReplaceIndex = -1;   // какой слот выбран для замены (подсвечен зелёным)

    void Start()
    {
        detailPanel.SetActive(false);
        if (cancelReplaceButton != null)
            cancelReplaceButton.SetActive(false);
        UpdateSlots();
    }

    public void UpdateSlots()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            if (i < PlayerInventory.abilities.Count)
            {
                slotImages[i].sprite = PlayerInventory.abilities[i].icon;
                // Цвет в зависимости от режима и выбора
                if (replaceMode)
                {
                    if (i == selectedReplaceIndex)
                        slotImages[i].color = Color.green;   // выбран для замены
                    else
                        slotImages[i].color = new Color(1f, 1f, 0.5f); // доступен для выбора
                }
                else
                {
                    slotImages[i].color = Color.white;
                }
            }
            else
            {
                slotImages[i].sprite = emptySlotSprite;
                slotImages[i].color = Color.white;
            }

            Button btn = slotImages[i].GetComponent<Button>();
            if (btn == null)
                btn = slotImages[i].gameObject.AddComponent<Button>();

            btn.onClick.RemoveAllListeners();
            int index = i;

            if (replaceMode && index < PlayerInventory.abilities.Count)
            {
                btn.onClick.AddListener(() => OnSlotClickedForReplace(index));
            }
            else if (!replaceMode)
            {
                btn.onClick.AddListener(() => OnSlotClicked(index));
            }
        }

        if (cancelReplaceButton != null)
        {
            cancelReplaceButton.SetActive(replaceMode);
            if (replaceMode)
            {
                cancelReplaceButton.GetComponent<Button>().onClick.RemoveAllListeners();
                cancelReplaceButton.GetComponent<Button>().onClick.AddListener(CancelReplaceMode);
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
        detailDescriptionText.text = ItemHandler.GetAbilityCustomDescription(ability);
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
            // Подсвечиваем выбранный слот и сразу вызываем замену
            selectedReplaceIndex = index;
            UpdateSlots();
            onReplaceCallback?.Invoke(PlayerInventory.abilities[index]);
        }
    }

    public void StartReplaceMode(Action<AbilityData> callback)
    {
        replaceMode = true;
        onReplaceCallback = callback;
        selectedReplaceIndex = -1;
        CloseDetail();
        UpdateSlots();
    }

    public void CancelReplaceMode()
    {
        replaceMode = false;
        onReplaceCallback = null;
        selectedReplaceIndex = -1;
        UpdateSlots();
        OnCancelReplace?.Invoke();
    }
}