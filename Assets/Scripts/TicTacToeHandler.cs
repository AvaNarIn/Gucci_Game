using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TicTacToeHandler : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private float animationDuration = 1.0f;
    public void PlayRound()
    {
        StartCoroutine(RoundCoroutine());
    }

    private IEnumerator RoundCoroutine()
    {
        ItemData[] gridState = gridManager.GetGridState();

        List<TicTacToeData> marks = new List<TicTacToeData>();
        int[] positions = new int[9];
        for (int i = 0; i < 9; i++)
        {
            if (gridState[i] is TicTacToeData mark)
            {
                marks.Add(mark);
                positions[i] = marks.Count - 1;
            }
            else
            {
                positions[i] = -1;
            }
        }

        int[][] lines = new int[][]
        {
            new int[] {0,1,2}, // верхняя строка
            new int[] {3,4,5}, // средняя строка
            new int[] {6,7,8}, // нижняя строка
            new int[] {0,3,6}, // левый столбец
            new int[] {1,4,7}, // средний столбец
            new int[] {2,5,8}, // правый столбец
            new int[] {0,4,8}, // главная диагональ
            new int[] {2,4,6}  // побочная диагональ
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

            if (marks[m0].type == marks[m1].type && marks[m1].type == marks[m2].type)
            {
                winningLines.Add(line);
            }
        }

        yield return new WaitForSeconds(animationDuration); //ЗАГЛУШКА ПОД АНИМАЦИЮ

 
        float score = CalculateScore(marks, positions, winningLines);
        Debug.Log($"Очки за крестики-нолики: {score}");
    }

    private float CalculateScore(List<TicTacToeData> marks, int[] positions, List<int[]> winningLines)
    {
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

        float total = 0f;
        for (int i = 0; i < marks.Count; i++)
        {
            float multiplier = Mathf.Pow(1.5f, lineCount[i]);
            total += marks[i].score * multiplier;
        }

        return total;
    }
}