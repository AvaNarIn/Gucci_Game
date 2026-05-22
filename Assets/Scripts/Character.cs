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
    public System.Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(int damage)
    {
        GetComponent<DamageFeedback>()?.Play();

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        UpdateUI();
        if (currentHealth <= 0)
            OnDeath?.Invoke();
    }

    public bool IsAlive => currentHealth > 0;

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (healthText != null)
            healthText.text = GameUtils.FormatNumber(currentHealth);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }

    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = newMaxHealth;
        UpdateUI();
    }
}