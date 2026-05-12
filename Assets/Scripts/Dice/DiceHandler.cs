using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceHandler : ItemHandler
{
    public override IEnumerator ApplyingEffects_Coroutine()
    {
        yield return new WaitForSeconds(animationDuration);
    }

    public override IEnumerator CountingScore_Coroutine()
    {
        List<DiceData> diceList = new List<DiceData>();
        List<int> diceIndices = new List<int>();
        ItemData[] gridState = gridManager.GetGridState();
        for (int i = 0; i < gridState.Length; i++)
        {
            if (gridState[i] is DiceData dice)
            {
                diceList.Add(dice);
                diceIndices.Add(i);
            }
        }

        yield return new WaitForSeconds(animationDuration);

        List<int> rolledValues = new List<int>();
        foreach (var dice in diceList)
        {
            int roll = Random.Range(1, (int)(dice.numberOfFaces) + 1);
            rolledValues.Add(roll);
        }

        float totalScore = CalculateScore(diceList, rolledValues, diceIndices);
        LastScore = totalScore;
    }

    private float CalculateScore(List<DiceData> diceList, List<int> values, List<int> indices)
    {
        Dictionary<int, int> counts = new Dictionary<int, int>();
        foreach (int v in values)
        {
            if (counts.ContainsKey(v))
                counts[v]++;
            else
                counts[v] = 1;
        }

        float total = 0f;
        GridCell[] cells = gridManager.GetCells();
        for (int i = 0; i < diceList.Count; i++)
        {
            DiceData dice = diceList[i];
            int value = values[i];
            int count = counts[value];
            int matches = count - 1;
            float multiplier = 1f + matches * 0.125f;
            GridCell cell = cells[indices[i]];
            float cellMult = cell.GetMultiplier(dice);
            total += dice.score * multiplier * cellMult;
        }

        return total;
    }
}