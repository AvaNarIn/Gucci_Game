using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TicTacToeHandler : ItemHandler
{
    public override IEnumerator ApplyingEffects_Coroutine()
    {
        yield return new WaitForSeconds(animationDuration);
    }

    public override IEnumerator CountingScore_Coroutine()
    {
        ItemData[] gridState = gridManager.GetGridState();
        GridCell[] cells = gridManager.GetCells();
        List<TicTacToeData> marks = new List<TicTacToeData>();
        List<int> markIndices = new List<int>();
        List<Draggable> markDraggables = new List<Draggable>();
        int[] positions = new int[9];

        for (int i = 0; i < 9; i++)
        {
            if (gridState[i] is TicTacToeData mark)
            {
                marks.Add(mark);
                markIndices.Add(i);
                markDraggables.Add(cells[i].currentItem);
                positions[i] = marks.Count - 1;
            }
            else positions[i] = -1;
        }

        int[][] lines = new int[][]
        {
            new int[] {0,1,2},
            new int[] {3,4,5},
            new int[] {6,7,8},
            new int[] {0,3,6},
            new int[] {1,4,7},
            new int[] {2,5,8},
            new int[] {0,4,8},
            new int[] {2,4,6}
        };

        List<int[]> winningLines = new List<int[]>();
        foreach (var line in lines)
        {
            int i0 = line[0], i1 = line[1], i2 = line[2];
            int m0 = positions[i0];
            int m1 = positions[i1];
            int m2 = positions[i2];

            if (m0 == -1 || m1 == -1 || m2 == -1)
                continue;

            if (marks[m0].markType == marks[m1].markType && marks[m1].markType == marks[m2].markType)
            {
                winningLines.Add(line);
            }
        }

        int[] lineCount = new int[marks.Count];
        foreach (var line in winningLines)
        {
            int m0 = positions[line[0]];
            int m1 = positions[line[1]];
            int m2 = positions[line[2]];
            lineCount[m0]++;
            lineCount[m1]++;
            lineCount[m2]++;
        }

        yield return new WaitForSeconds(animationDuration);

        float totalScore = CalculateScore(marks, positions, lineCount, markIndices, markDraggables);

        // Базовая способность
        if (HasAbility("Базовое усиление (Крестики-Нолики)"))
            totalScore *= 1.5f;

        // Новые способности
        if (HasAbility("Диагональное усиление"))
        {
            int centerIdx = positions[4]; // индекс в marks для центральной клетки
            if (centerIdx >= 0 && marks[centerIdx].markType == TicTacToeData.MarkTypes.Cross)
            {
                float sum = 0f;
                int[] diagIndices = { 0, 2, 6, 8 };
                foreach (int cellIdx in diagIndices)
                {
                    int mIdx = positions[cellIdx];
                    if (mIdx >= 0)
                    {
                        // Используем GetPieceScore для учёта клетки и линий
                        sum += GetPieceScore(marks[mIdx], cellIdx, lineCount[mIdx]);
                    }
                }
                totalScore += sum * 0.5f;
            }
        }

        if (HasAbility("Прямое усиление"))
        {
            int centerIdx = positions[4];
            if (centerIdx >= 0 && marks[centerIdx].markType == TicTacToeData.MarkTypes.Nought)
            {
                float sum = 0f;
                int[] straightIndices = { 1, 3, 5, 7 };
                foreach (int cellIdx in straightIndices)
                {
                    int mIdx = positions[cellIdx];
                    if (mIdx >= 0)
                    {
                        sum += GetPieceScore(marks[mIdx], cellIdx, lineCount[mIdx]);
                    }
                }
                totalScore += sum * 0.5f;
            }
        }

        LastScore = totalScore;
    }

    private float GetPieceScore(TicTacToeData mark, int cellIndex, int lineCount)
    {
        float multiplier = Mathf.Pow(1.5f, lineCount);
        GridCell cell = gridManager.GetCells()[cellIndex];
        float cellMult = cell.GetMultiplier(mark);
        return mark.score * multiplier * cellMult;
    }

    private float CalculateScore(List<TicTacToeData> marks, int[] positions, int[] lineCount, List<int> indices, List<Draggable> draggables)
    {
        float total = 0f;
        GridCell[] cells = gridManager.GetCells();
        for (int i = 0; i < marks.Count; i++)
        {
            TicTacToeData mark = marks[i];
            float multiplier = Mathf.Pow(1.5f, lineCount[i]);
            GridCell cell = cells[indices[i]];
            float cellMult = cell.GetMultiplier(mark);
            float pieceScore = mark.score * multiplier * cellMult;
            total += pieceScore;

            if (draggables[i] != null)
                draggables[i].ShowScoreGain(Mathf.RoundToInt(pieceScore));
        }
        return total;
    }
}