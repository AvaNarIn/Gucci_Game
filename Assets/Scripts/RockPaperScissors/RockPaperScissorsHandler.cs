using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockPaperScissorsHandler : ItemHandler
{
    private static readonly Dictionary<RockPaperScissorsData.Shapes, RockPaperScissorsData.Shapes> beats =
        new Dictionary<RockPaperScissorsData.Shapes, RockPaperScissorsData.Shapes>
        {
            { RockPaperScissorsData.Shapes.Rock, RockPaperScissorsData.Shapes.Scissors },
            { RockPaperScissorsData.Shapes.Scissors, RockPaperScissorsData.Shapes.Paper },
            { RockPaperScissorsData.Shapes.Paper, RockPaperScissorsData.Shapes.Rock }
        };

    private bool Beats(RockPaperScissorsData.Shapes attacker, RockPaperScissorsData.Shapes defender)
    {
        return beats.ContainsKey(attacker) && beats[attacker] == defender;
    }

    public override IEnumerator ApplyingEffects_Coroutine()
    {
        yield return new WaitForSeconds(animationDuration);
    }

    public override IEnumerator CountingScore_Coroutine()
    {
        ItemData[] gridState = gridManager.GetGridState();
        Dictionary<int, RockPaperScissorsData> cellToItem = new Dictionary<int, RockPaperScissorsData>();
        List<int> indices = new List<int>();

        for (int i = 0; i < gridState.Length; i++)
        {
            if (gridState[i] is RockPaperScissorsData rps)
            {
                cellToItem.Add(i, rps);
                indices.Add(i);
            }
        }

        yield return new WaitForSeconds(animationDuration); //«ј√Ћ”Ў ј ƒЋя јЌ»ћј÷»»

        float totalScore = 0f;

        foreach (int index in indices)
        {
            RockPaperScissorsData current = cellToItem[index];
            int row = index / 3;
            int col = index % 3;

            int beatenNeighbours = 0;

            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;

                    int r = row + dr;
                    int c = col + dc;

                    if (r >= 0 && r < 3 && c >= 0 && c < 3)
                    {
                        int neighbourIndex = r * 3 + c;
                        if (cellToItem.TryGetValue(neighbourIndex, out RockPaperScissorsData neighbour))
                        {
                            if (Beats(current.shape, neighbour.shape))
                                beatenNeighbours++;
                        }
                    }
                }
            }

            float multiplier = Mathf.Pow(1.1f, beatenNeighbours);
            float itemScore = current.score * multiplier;
            totalScore += itemScore;

            Debug.Log($"{current.shape} ({current.score}) побил {beatenNeighbours} соседей, " +
                      $"множитель {multiplier:F4}, очки: {itemScore:F2}");
        }

        Debug.Log($"ќбщий счЄт камней-ножниц-бумаг: {totalScore}");
    }
}