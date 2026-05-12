using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Character : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private Text healthText;

    private int currentHealth;
    public int CurrentHealth => currentHealth;

    public System.Action<Character> OnClicked;

    private void Awake()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        UpdateUI();
    }

    public bool IsAlive => currentHealth > 0;

    private void UpdateUI()
    {
        if (healthText != null)
            healthText.text = $"HP: {currentHealth}/{maxHealth}";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }
}