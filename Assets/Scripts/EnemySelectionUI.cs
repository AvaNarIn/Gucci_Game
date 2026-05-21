using UnityEngine;
using UnityEngine.UI;

public class EnemySelectionUI : MonoBehaviour
{
    public EnemyCard[] enemyCards;

    public void Show(EnemyInfo[] enemies)
    {
        gameObject.SetActive(true);
        for (int i = 0; i < enemyCards.Length; i++)
        {
            if (i < enemies.Length)
            {
                enemyCards[i].gameObject.SetActive(true);
                enemyCards[i].Setup(enemies[i], i);
            }
            else
            {
                enemyCards[i].gameObject.SetActive(false);
            }
        }
    }
}

[System.Serializable]
public class EnemyCard
{
    public GameObject gameObject;
    public Text abilityText;
    public Text set1Text;
    public Text set2Text;
    public Text rewardsText;
    public Text healthText;
    public Button selectButton;

    public void Setup(EnemyInfo enemy, int index)
    {
        abilityText.text = enemy.isBoss ? enemy.abilityDescription : "Нет способности";
        set1Text.text = enemy.set1.ToString();
        set2Text.text = enemy.set2.ToString();
        rewardsText.text = "";
        foreach (var r in enemy.rewards)
            rewardsText.text += r.ToString() + " ";
        healthText.text = "HP: " + enemy.health;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => MetaGameManager.Instance.OnEnemySelected(index));
    }
}