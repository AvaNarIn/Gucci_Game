using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceHandler : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private float animationDuration = 1.5f;

    public void PlayRound()
    {
        StartCoroutine(RoundCoroutine());
    }

    private IEnumerator RoundCoroutine()
    {
        List<DiceData> diceList = new List<DiceData>();
        ItemData[] gridState = gridManager.GetGridState();
        foreach (var item in gridState)
        {
            if (item is DiceData dice)
                diceList.Add(dice);
        }

        yield return new WaitForSeconds(animationDuration); //ЗАГЛУШКА ПОД АНИМАЦИЮ

        List<int> rolledValues = new List<int>();
        foreach (var dice in diceList)
        {
            int roll = Random.Range(1, dice.numberOfFaces + 1);
            Debug.Log($"Очки за один кубик: {roll}");
            rolledValues.Add(roll);
        }

        float score = CalculateScore(diceList, rolledValues);
        Debug.Log($"Очки за кубики: {score}");
    }

    private float CalculateScore(List<DiceData> diceList, List<int> values)
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
        for (int i = 0; i < diceList.Count; i++)
        {
            DiceData dice = diceList[i];
            int value = values[i];
            int count = counts[value];
            int matches = count - 1;
            float multiplier = 1f + matches * 0.125f;
            total += dice.score * multiplier;
        }

        return total;
    }
}