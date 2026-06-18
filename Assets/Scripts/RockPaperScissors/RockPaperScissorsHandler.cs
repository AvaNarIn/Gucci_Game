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
        GridCell[] cells = gridManager.GetCells();
        Dictionary<int, RockPaperScissorsData> cellToItem = new Dictionary<int, RockPaperScissorsData>();
        List<int> indices = new List<int>();
        List<Draggable> draggables = new List<Draggable>();

        // Подсчитываем количество каждого типа на поле
        int rockCount = 0, scissorsCount = 0, paperCount = 0;

        for (int i = 0; i < gridState.Length; i++)
        {
            if (gridState[i] is RockPaperScissorsData rps)
            {
                cellToItem.Add(i, rps);
                indices.Add(i);
                draggables.Add(cells[i].currentItem);

                switch (rps.shape)
                {
                    case RockPaperScissorsData.Shapes.Rock: rockCount++; break;
                    case RockPaperScissorsData.Shapes.Scissors: scissorsCount++; break;
                    case RockPaperScissorsData.Shapes.Paper: paperCount++; break;
                }
            }
        }

        yield return new WaitForSeconds(animationDuration);

        float totalScore = 0f;

        for (int idx = 0; idx < indices.Count; idx++)
        {
            int index = indices[idx];
            RockPaperScissorsData current = cellToItem[index];
            int row = index / 3;
            int col = index % 3;

            int beatenNeighbours = 0;

            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int r = row + dr;
                    int c = col + dc;
                    if (r < 0 || r >= 3 || c < 0 || c >= 3) continue;

                    int neighbourIndex = r * 3 + c;
                    if (cellToItem.TryGetValue(neighbourIndex, out RockPaperScissorsData neighbour))
                    {
                        if (Beats(current.shape, neighbour.shape))
                            beatenNeighbours++;
                    }
                }

            float multiplier = Mathf.Pow(1.1f, beatenNeighbours);
            GridCell cell = cells[index];
            float cellMult = cell.GetMultiplier(current);
            float itemScore = current.score * multiplier * cellMult;

            // Новые способности "Единый камень", "Единые ножницы", "Единая бумага"
            if (HasAbility("Единый камень") && current.shape == RockPaperScissorsData.Shapes.Rock && rockCount == 1)
                itemScore *= 2f;
            if (HasAbility("Единые ножницы") && current.shape == RockPaperScissorsData.Shapes.Scissors && scissorsCount == 1)
                itemScore *= 2f;
            if (HasAbility("Единая бумага") && current.shape == RockPaperScissorsData.Shapes.Paper && paperCount == 1)
                itemScore *= 2f;

            totalScore += itemScore;

            if (draggables[idx] != null)
                draggables[idx].ShowScoreGain(Mathf.RoundToInt(itemScore));
        }

        if (HasAbility("Базовое усиление (КНБ)"))
            totalScore *= 1.5f;

        LastScore = totalScore;
    }
}