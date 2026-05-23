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

    public void OnBotTurnEnd(GridManager playerGridManager)
    {
        if (currentBossAbility == null) return;

        string abilityName = currentBossAbility.abilityName;
        System.Type targetType = null;

        if (abilityName == "Анти-Карты") targetType = typeof(CardData);
        else if (abilityName == "Анти-Кубики") targetType = typeof(DiceData);
        else if (abilityName == "Анти-Шахматы") targetType = typeof(ChessData);
        else if (abilityName == "Анти-КНБ") targetType = typeof(RockPaperScissorsData);
        else if (abilityName == "Анти-Крестики-Нолики") targetType = typeof(TicTacToeData);

        if (targetType == null) return;

        var targetCells = new List<GridCell>();
        foreach (var cell in playerGridManager.GetCells())
        {
            if (cell.currentItem != null && cell.currentItem.ItemData != null &&
                targetType.IsAssignableFrom(cell.currentItem.ItemData.GetType()))
            {
                targetCells.Add(cell);
            }
        }

        if (targetCells.Count > 0)
        {
            GridCell target = targetCells[Random.Range(0, targetCells.Count)];
            target.TakeDamage(target.CurrentHealth);
            Debug.Log($"[{abilityName}] Уничтожен предмет игрока в клетке {target.CellIndex}");
        }
    }
}