using System.Collections.Generic;
using UnityEngine;

public class BotAbilityHandler : MonoBehaviour
{
    private BossAbilityData currentBossAbility;

    public void SetBossAbility(BossAbilityData ability)
    {
        currentBossAbility = ability;
        if (ability != null)
            Debug.Log($"Способность босса: {ability.abilityName}");
        else
            Debug.Log("Обычный противник, способности нет");
    }

    /// <summary>Вызывается в конце хода бота.</summary>
    public void OnBotTurnEnd(GridManager playerGridManager)
    {
        if (currentBossAbility == null) return;

        if (currentBossAbility.abilityName == "Анти Карты")
        {
            var cardCells = new List<GridCell>();
            foreach (var cell in playerGridManager.GetCells())
            {
                if (cell.currentItem != null && cell.currentItem.ItemData is CardData)
                    cardCells.Add(cell);
            }

            if (cardCells.Count > 0)
            {
                GridCell target = cardCells[Random.Range(0, cardCells.Count)];
                target.TakeDamage(target.CurrentHealth);   // наносим урон, равный здоровью клетки
                Debug.Log($"[Анти Карты] Уничтожена карта игрока в клетке {target.CellIndex}");
            }
        }
    }
}