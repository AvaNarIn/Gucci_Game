using UnityEngine;

public class BotAbilityHandler : MonoBehaviour
{
    private EnemyInfo currentEnemy;

    public void SetBossAbility(EnemyInfo enemy)
    {
        currentEnemy = enemy;
        if (enemy.isBoss)
        {
            Debug.Log($"Способность босса: {enemy.abilityDescription}");
        }
    }
}