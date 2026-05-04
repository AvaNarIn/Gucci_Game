using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChessHandler : ItemHandler
{
    public override IEnumerator ApplyingEffects_Coroutine()
    {
        yield return new WaitForSeconds(animationDuration);
    }

    public override IEnumerator CountingScore_Coroutine()
    {
        ItemData[] gridState = gridManager.GetGridState();
        List<ChessData> pieces = new List<ChessData>();
        List<int> pieceIndices = new List<int>();
        for (int i = 0; i < gridState.Length; i++)
        {
            if (gridState[i] is ChessData chess)
            {
                pieces.Add(chess);
                pieceIndices.Add(i);
            }
        }

        yield return new WaitForSeconds(animationDuration); //ЗАГЛУШКА ДЛЯ АНИМАЦИИ

        float totalScore = CalculateScore(pieces, pieceIndices);
        Debug.Log($"Очки за шахматы: {totalScore}");
    }

    private float CalculateScore(List<ChessData> pieces, List<int> indices)
    {
        if (pieces.Count == 0) return 0f;

        int[] rows = indices.Select(i => i / 3).ToArray();
        int[] cols = indices.Select(i => i % 3).ToArray();

        List<List<Vector2Int>> attackedByPiece = new List<List<Vector2Int>>();

        for (int i = 0; i < pieces.Count; i++)
        {
            List<Vector2Int> attacked = GetAttackedCells(
                pieces[i].TypeOfChessPiece,
                rows[i], cols[i],
                rows, cols 
            );
            attackedByPiece.Add(attacked);
        }

        float total = 0f;
        for (int i = 0; i < pieces.Count; i++)
        {
            int targets = 0;
            for (int j = 0; j < pieces.Count; j++)
            {
                if (i == j) continue;
                Vector2Int pos = new Vector2Int(rows[j], cols[j]);
                if (attackedByPiece[i].Contains(pos))
                    targets++;
            }
            float multiplier = Mathf.Pow(1.25f, targets);
            float pieceScore = pieces[i].score * multiplier;
            total += pieceScore;

            Debug.Log($"Фигура {pieces[i].TypeOfChessPiece} ({pieces[i].score} x {multiplier:F4} = {pieceScore:F4})");
        }

        return total;
    }

    private List<Vector2Int> GetAttackedCells(
        ChessData.TypesOfChessPiece type,
        int row, int col,
        int[] allRows, int[] allCols)
    {
        List<Vector2Int> attacked = new List<Vector2Int>();
        HashSet<Vector2Int> allPositions = new HashSet<Vector2Int>();
        for (int k = 0; k < allRows.Length; k++)
            allPositions.Add(new Vector2Int(allRows[k], allCols[k]));

        // Направления для слона и ферзя
        int[][] diagDirs = new int[][] { new int[] { 1, 1 }, new int[] { 1, -1 },
                                     new int[] { -1, 1 }, new int[] { -1, -1 } };
        // Направления для ладьи и ферзя
        int[][] rookDirs = new int[][] { new int[] { 0, 1 }, new int[] { 0, -1 },
                                     new int[] { 1, 0 }, new int[] { -1, 0 } };

        switch (type)
        {
            case ChessData.TypesOfChessPiece.Pawn:
                if (row - 1 >= 0)
                {
                    if (col - 1 >= 0) attacked.Add(new Vector2Int(row - 1, col - 1));
                    if (col + 1 < 3) attacked.Add(new Vector2Int(row - 1, col + 1));
                }
                break;

            case ChessData.TypesOfChessPiece.Knight:
                int[] dr = { -2, -2, -1, -1, 1, 1, 2, 2 };
                int[] dc = { -1, 1, -2, 2, -2, 2, -1, 1 };
                for (int k = 0; k < 8; k++)
                {
                    int r = row + dr[k];
                    int c = col + dc[k];
                    if (r >= 0 && r < 3 && c >= 0 && c < 3)
                        attacked.Add(new Vector2Int(r, c));
                }
                break;

            case ChessData.TypesOfChessPiece.Bishop:
                foreach (var dir in diagDirs)
                {
                    int r = row + dir[0];
                    int c = col + dir[1];
                    while (r >= 0 && r < 3 && c >= 0 && c < 3)
                    {
                        Vector2Int cell = new Vector2Int(r, c);
                        attacked.Add(cell);
                        if (allPositions.Contains(cell)) break;
                        r += dir[0];
                        c += dir[1];
                    }
                }
                break;

            case ChessData.TypesOfChessPiece.Rook:
                foreach (var dir in rookDirs)
                {
                    int r = row + dir[0];
                    int c = col + dir[1];
                    while (r >= 0 && r < 3 && c >= 0 && c < 3)
                    {
                        Vector2Int cell = new Vector2Int(r, c);
                        attacked.Add(cell);
                        if (allPositions.Contains(cell)) break;
                        r += dir[0];
                        c += dir[1];
                    }
                }
                break;

            case ChessData.TypesOfChessPiece.Queen:
                foreach (var dir in diagDirs)
                {
                    int r = row + dir[0];
                    int c = col + dir[1];
                    while (r >= 0 && r < 3 && c >= 0 && c < 3)
                    {
                        Vector2Int cell = new Vector2Int(r, c);
                        attacked.Add(cell);
                        if (allPositions.Contains(cell)) break;
                        r += dir[0];
                        c += dir[1];
                    }
                }
                foreach (var dir in rookDirs)
                {
                    int r = row + dir[0];
                    int c = col + dir[1];
                    while (r >= 0 && r < 3 && c >= 0 && c < 3)
                    {
                        Vector2Int cell = new Vector2Int(r, c);
                        attacked.Add(cell);
                        if (allPositions.Contains(cell)) break;
                        r += dir[0];
                        c += dir[1];
                    }
                }
                break;

            case ChessData.TypesOfChessPiece.King:
                for (int dr2 = -1; dr2 <= 1; dr2++)
                {
                    for (int dc2 = -1; dc2 <= 1; dc2++)
                    {
                        if (dr2 == 0 && dc2 == 0) continue;
                        int r = row + dr2;
                        int c = col + dc2;
                        if (r >= 0 && r < 3 && c >= 0 && c < 3)
                            attacked.Add(new Vector2Int(r, c));
                    }
                }
                break;
        }

        return attacked;
    }
}