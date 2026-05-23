using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BotController : MonoBehaviour
{
    [SerializeField] private GridManager botGridManager;
    [SerializeField] private Transform handPanel;
    [SerializeField] private float placementDelay = 1f;
    [SerializeField] private float actionDelay = 1f;

    [Header("Placement - Weights")]
    [SerializeField] private float baseValueWeight = 1.0f;
    [SerializeField] private float sameSetSynergyWeight = 0.20f;
    [SerializeField] private float adjacencySynergyWeight = 0.12f;
    [SerializeField] private float positionalWeight = 0.08f;
    [SerializeField] private float predictedDeltaWeight = 0.75f;

    [Header("Placement - Variety")]
    [SerializeField] private int placementTopK = 3;
    [Range(0f, 1f)]
    [SerializeField] private float placementExploreChance = 0.20f;

    [Header("Action - Face Hit Chances")]
    [SerializeField] private int faceLowHpThreshold = 12;
    [Range(0f, 1f)]
    [SerializeField] private float faceHitChanceHighHp = 0.10f;
    [Range(0f, 1f)]
    [SerializeField] private float faceHitChanceLowHp = 0.30f;

    [Header("Action - Targeting")]
    [SerializeField] private float canKillNowBonus = 0.20f;

    [Header("Action - Variety")]
    [SerializeField] private int attackTopK = 2;
    [Range(0f, 1f)]
    [SerializeField] private float attackExploreChance = 0.12f;

    [Header("Noise")]
    [SerializeField] private float randomNoise = 0.01f;

    private readonly List<Draggable> handItems = new List<Draggable>();
    private Action onPlacementComplete;
    private Action<int> onActionComplete;

    private System.Random botRng;

    private void Awake()
    {
        botRng = new System.Random();
    }

    private float Rng01() => (float)botRng.NextDouble();
    private float RngRange(float min, float max) => min + (max - min) * Rng01();

    public void StartPlacementPhase(Action onComplete)
    {
        StopAllCoroutines();

        onPlacementComplete = onComplete;
        RefreshHandItemsFromPanel();

        if (handItems.Count >= 6)
        {
            Draggable cheapest = FindCheapest(handItems);
            if (cheapest != null)
            {
                cheapest.DestroyItem();
                TurnManager.Instance.AddBotMana(1);
                handItems.Remove(cheapest);
            }
        }

        StartCoroutine(PlacementCoroutine());
    }

    private IEnumerator PlacementCoroutine()
    {
        while (true)
        {
            if (handItems.Count == 0) break;

            List<int> emptyCells = botGridManager.GetEmptyCells();
            if (emptyCells == null || emptyCells.Count == 0) break;

            List<Draggable> affordable = handItems.FindAll(d =>
                d != null && d.ItemData != null && TurnManager.Instance.CanAffordBot(d.ItemData.score));

            if (affordable.Count == 0) break;

            bool found = TryChooseBestPlacement(affordable, emptyCells, out Draggable chosen, out int targetCell);
            if (!found)
            {
                chosen = affordable[botRng.Next(0, affordable.Count)];
                targetCell = emptyCells[botRng.Next(0, emptyCells.Count)];
            }

            botGridManager.PlaceExistingDraggable(chosen, targetCell);
            handItems.Remove(chosen);

            yield return new WaitForSeconds(placementDelay);
        }

        onPlacementComplete?.Invoke();
    }

    private struct PlacementOption
    {
        public Draggable card;
        public int cellIndex;
        public float score;
    }

    private bool TryChooseBestPlacement(List<Draggable> affordable, List<int> emptyCells, out Draggable bestCard, out int bestCellIndex)
    {
        bestCard = null;
        bestCellIndex = -1;

        GridCell[] cells = botGridManager.GetCells();
        ItemData[] currentState = botGridManager.GetGridState();
        if (cells == null || currentState == null) return false;

        Dictionary<ItemSet, int> setCounts = CountSetsOnField(currentState);
        List<PlacementOption> options = new List<PlacementOption>(affordable.Count * emptyCells.Count);

        foreach (var d in affordable)
        {
            ItemData item = d.ItemData;
            ItemSet set = GetItemSet(item);
            float beforeSetScore = EstimateSetScore(set, currentState, cells);

            foreach (int idx in emptyCells)
            {
                if (idx < 0 || idx >= 9) continue;
                GridCell cell = cells[idx];
                if (cell == null) continue;

                float s = EvaluatePlacement(item, set, idx, cell, currentState, cells, setCounts, beforeSetScore);
                options.Add(new PlacementOption { card = d, cellIndex = idx, score = s });
            }
        }

        if (options.Count == 0) return false;

        PlacementOption chosen = ChoosePlacement(options);
        bestCard = chosen.card;
        bestCellIndex = chosen.cellIndex;
        return bestCard != null && bestCellIndex >= 0;
    }

    private PlacementOption ChoosePlacement(List<PlacementOption> options)
    {
        options.Sort((a, b) => b.score.CompareTo(a.score));

        int k = Mathf.Clamp(placementTopK, 1, options.Count);
        if (k == 1) return options[0];

        if (Rng01() >= placementExploreChance)
            return options[0];

        float min = float.PositiveInfinity;
        for (int i = 0; i < k; i++) min = Mathf.Min(min, options[i].score);

        float sum = 0f;
        float[] w = new float[k];
        for (int i = 0; i < k; i++)
        {
            w[i] = Mathf.Max(0.001f, options[i].score - min + 0.001f);
            sum += w[i];
        }

        float r = Rng01() * sum;
        float acc = 0f;
        for (int i = 0; i < k; i++)
        {
            acc += w[i];
            if (r <= acc) return options[i];
        }

        return options[0];
    }

    private float EvaluatePlacement(
        ItemData item,
        ItemSet set,
        int placeIndex,
        GridCell placeCell,
        ItemData[] currentState,
        GridCell[] cells,
        Dictionary<ItemSet, int> setCounts,
        float beforeSetScore)
    {
        float cellMult = placeCell.GetMultiplier(item);
        float baseValue = item.score * cellMult;

        int sameSetCount = 0;
        setCounts.TryGetValue(set, out sameSetCount);
        float sameSetSynergy = baseValue * sameSetCount;

        int adjSameSet = CountAdjacentSameSet8(placeIndex, currentState, set);
        float adjacencySynergy = baseValue * adjSameSet;

        float positional = baseValue * PositionalFactor(placeIndex, set);

        ItemData[] temp = (ItemData[])currentState.Clone();
        temp[placeIndex] = item;

        float afterSetScore = EstimateSetScore(set, temp, cells);
        float predictedDelta = afterSetScore - beforeSetScore;

        float value =
            baseValueWeight * baseValue +
            sameSetSynergyWeight * sameSetSynergy +
            adjacencySynergyWeight * adjacencySynergy +
            positionalWeight * positional +
            predictedDeltaWeight * predictedDelta;

        value += RngRange(0f, randomNoise);
        return value;
    }

    private float PositionalFactor(int index, ItemSet set)
    {
        bool isCenter = index == 4;
        bool isCorner = (index == 0 || index == 2 || index == 6 || index == 8);

        switch (set)
        {
            case ItemSet.Chess: return isCenter ? 0.25f : (isCorner ? 0.10f : 0.15f);
            case ItemSet.TicTacToe: return isCenter ? 0.22f : (isCorner ? 0.14f : 0.08f);
            case ItemSet.RockPaperScissors: return isCenter ? 0.18f : (isCorner ? 0.07f : 0.12f);
            default: return 0.05f;
        }
    }

    private int CountAdjacentSameSet8(int index, ItemData[] state, ItemSet set)
    {
        int row = index / 3;
        int col = index % 3;

        int count = 0;
        for (int dr = -1; dr <= 1; dr++)
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int r = row + dr;
                int c = col + dc;
                if (r < 0 || r >= 3 || c < 0 || c >= 3) continue;

                int ni = r * 3 + c;
                ItemData it = state[ni];
                if (it == null) continue;
                if (GetItemSet(it) == set) count++;
            }
        return count;
    }

    public void StartActionPhase(Action<int> onComplete, int attackScore, GridManager playerGridManager, Character playerCharacter)
    {
        StopAllCoroutines();

        onActionComplete = onComplete;
        StartCoroutine(ActionCoroutine(attackScore, playerGridManager, playerCharacter));
    }

    private IEnumerator ActionCoroutine(int attackScore, GridManager playerGridManager, Character playerCharacter)
    {
        int remainingScore = Mathf.Max(0, attackScore);

        if (remainingScore <= 0 || playerGridManager == null || playerCharacter == null)
        {
            onActionComplete?.Invoke(remainingScore);
            yield break;
        }

        while (remainingScore > 0 && (HasAnyEnemyCell(playerGridManager) || playerCharacter.IsAlive))
        {
            // 1. Добивание персонажа, если хватает очков
            if (playerCharacter.IsAlive && remainingScore >= playerCharacter.CurrentHealth)
            {
                int dmg = playerCharacter.CurrentHealth;
                playerCharacter.TakeDamage(dmg);
                remainingScore -= dmg;
                TurnManager.Instance.UpdateBotScoreDisplay(remainingScore);
                yield return new WaitForSeconds(actionDelay);
                continue;
            }

            bool hasCells = HasAnyEnemyCell(playerGridManager);

            // 2. Клеток нет — бьём персонажа
            if (!hasCells)
            {
                if (playerCharacter.IsAlive)
                {
                    int dmg = Mathf.Min(remainingScore, playerCharacter.CurrentHealth);
                    playerCharacter.TakeDamage(dmg);
                    remainingScore -= dmg;
                    TurnManager.Instance.UpdateBotScoreDisplay(remainingScore);
                }
                yield return new WaitForSeconds(actionDelay);
                continue;
            }

            // 3. Случайная атака персонажа
            float faceChance = GetFaceHitChance(playerCharacter);
            bool attackFace = playerCharacter.IsAlive && Rng01() < faceChance;

            if (attackFace)
            {
                int dmg = Mathf.Min(remainingScore, playerCharacter.CurrentHealth);
                playerCharacter.TakeDamage(dmg);
                remainingScore -= dmg;
                TurnManager.Instance.UpdateBotScoreDisplay(remainingScore);
                yield return new WaitForSeconds(actionDelay);
                continue;
            }

            // 4. Атака клетки (основной удар)
            GridCell target = ChooseBestEnemyCellToAttack(playerGridManager, remainingScore);
            if (target == null)
            {
                // На всякий случай бьём персонажа, если нет целей
                if (playerCharacter.IsAlive)
                {
                    int dmg = Mathf.Min(remainingScore, playerCharacter.CurrentHealth);
                    playerCharacter.TakeDamage(dmg);
                    remainingScore -= dmg;
                    TurnManager.Instance.UpdateBotScoreDisplay(remainingScore);
                }
                yield return new WaitForSeconds(actionDelay);
                continue;
            }

            int cellDmg = Mathf.Min(remainingScore, target.CurrentHealth);
            target.TakeDamage(cellDmg);
            remainingScore -= cellDmg;
            TurnManager.Instance.UpdateBotScoreDisplay(remainingScore);

            yield return new WaitForSeconds(actionDelay);
        }

        onActionComplete?.Invoke(remainingScore);
    }

    private float GetFaceHitChance(Character playerCharacter)
    {
        if (playerCharacter == null || !playerCharacter.IsAlive) return 0f;
        return playerCharacter.CurrentHealth <= faceLowHpThreshold ? faceHitChanceLowHp : faceHitChanceHighHp;
    }

    private struct CellAttackOption
    {
        public GridCell cell;
        public float value;
    }

    private GridCell ChooseBestEnemyCellToAttack(GridManager playerGridManager, int remainingScore)
    {
        GridCell[] cells = playerGridManager.GetCells();
        ItemData[] state = playerGridManager.GetGridState();
        if (cells == null || state == null) return null;

        float beforeTotal = EstimateTotalScore(state, cells);
        List<CellAttackOption> options = new List<CellAttackOption>();

        for (int i = 0; i < 9; i++)
        {
            GridCell cell = cells[i];
            if (cell == null || cell.CurrentHealth <= 0) continue;
            if (cell.currentItem == null || cell.currentItem.ItemData == null) continue;

            ItemData item = cell.currentItem.ItemData;
            bool canKillNow = cell.CurrentHealth <= remainingScore;

            ItemData[] temp = (ItemData[])state.Clone();
            temp[i] = null;

            float afterTotal = EstimateTotalScore(temp, cells);
            float reduction = beforeTotal - afterTotal;

            int deal = Mathf.Min(remainingScore, cell.CurrentHealth);
            float partialFactor = deal / (float)Mathf.Max(1, cell.CurrentHealth);
            float partialReduction = reduction * partialFactor;

            float rawThreat = item.score * cell.GetMultiplier(item);

            float value = (canKillNow ? reduction : partialReduction) + 0.08f * rawThreat;
            if (canKillNow) value += canKillNowBonus * rawThreat;

            value += RngRange(0f, randomNoise);

            options.Add(new CellAttackOption { cell = cell, value = value });
        }

        if (options.Count == 0) return null;

        options.Sort((a, b) => b.value.CompareTo(a.value));

        int k = Mathf.Clamp(attackTopK, 1, options.Count);
        if (k == 1) return options[0].cell;

        if (Rng01() >= attackExploreChance)
            return options[0].cell;

        float min = float.PositiveInfinity;
        for (int i = 0; i < k; i++) min = Mathf.Min(min, options[i].value);

        float sum = 0f;
        float[] w = new float[k];
        for (int i = 0; i < k; i++)
        {
            w[i] = Mathf.Max(0.001f, options[i].value - min + 0.001f);
            sum += w[i];
        }

        float r = Rng01() * sum;
        float acc = 0f;
        for (int i = 0; i < k; i++)
        {
            acc += w[i];
            if (r <= acc) return options[i].cell;
        }

        return options[0].cell;
    }

    private bool HasAnyEnemyCell(GridManager playerGridManager)
    {
        GridCell[] cells = playerGridManager.GetCells();
        if (cells == null) return false;

        foreach (var c in cells)
            if (c != null && c.currentItem != null && c.CurrentHealth > 0)
                return true;

        return false;
    }

    private float EstimateTotalScore(ItemData[] state, GridCell[] cells)
    {
        float total = 0f;
        total += EstimateDiceScore(state, cells);
        total += EstimateCardsScore(state, cells);
        total += EstimateChessScore(state, cells);
        total += EstimateRpsScore(state, cells);
        total += EstimateTttScore(state, cells);
        return total;
    }

    private float EstimateSetScore(ItemSet set, ItemData[] state, GridCell[] cells)
    {
        switch (set)
        {
            case ItemSet.Dice: return EstimateDiceScore(state, cells);
            case ItemSet.Card: return EstimateCardsScore(state, cells);
            case ItemSet.Chess: return EstimateChessScore(state, cells);
            case ItemSet.RockPaperScissors: return EstimateRpsScore(state, cells);
            case ItemSet.TicTacToe: return EstimateTttScore(state, cells);
            default: return 0f;
        }
    }

    private float EstimateDiceScore(ItemData[] state, GridCell[] cells)
    {
        List<(DiceData die, int idx)> dice = new();
        for (int i = 0; i < 9; i++)
            if (state[i] is DiceData d) dice.Add((d, i));

        if (dice.Count == 0) return 0f;

        float total = 0f;

        for (int i = 0; i < dice.Count; i++)
        {
            int Fi = (int)dice[i].die.numberOfFaces;

            float expectedMatches = 0f;
            for (int j = 0; j < dice.Count; j++)
            {
                if (i == j) continue;
                int Fj = (int)dice[j].die.numberOfFaces;
                int overlap = Mathf.Min(Fi, Fj);
                expectedMatches += (float)overlap / (Fi * Fj);
            }

            float multiplier = 1f + expectedMatches * 0.125f;
            float cellMult = cells[dice[i].idx].GetMultiplier(dice[i].die);
            total += dice[i].die.score * multiplier * cellMult;
        }

        return total;
    }

    private float EstimateRpsScore(ItemData[] state, GridCell[] cells)
    {
        Dictionary<int, RockPaperScissorsData> map = new Dictionary<int, RockPaperScissorsData>();
        List<int> indices = new List<int>();

        for (int i = 0; i < 9; i++)
        {
            if (state[i] is RockPaperScissorsData rps)
            {
                map[i] = rps;
                indices.Add(i);
            }
        }

        if (indices.Count == 0) return 0f;

        float total = 0f;

        foreach (int index in indices)
        {
            RockPaperScissorsData current = map[index];
            int row = index / 3;
            int col = index % 3;

            int beaten = 0;

            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;

                    int r = row + dr;
                    int c = col + dc;
                    if (r < 0 || r >= 3 || c < 0 || c >= 3) continue;

                    int ni = r * 3 + c;
                    if (map.TryGetValue(ni, out RockPaperScissorsData neighbour))
                        if (Beats(current.shape, neighbour.shape))
                            beaten++;
                }

            float multiplier = Mathf.Pow(1.1f, beaten);
            float cellMult = cells[index].GetMultiplier(current);
            total += current.score * multiplier * cellMult;
        }

        return total;
    }

    private static readonly Dictionary<RockPaperScissorsData.Shapes, RockPaperScissorsData.Shapes> rpsBeats =
        new Dictionary<RockPaperScissorsData.Shapes, RockPaperScissorsData.Shapes>
        {
            { RockPaperScissorsData.Shapes.Rock, RockPaperScissorsData.Shapes.Scissors },
            { RockPaperScissorsData.Shapes.Scissors, RockPaperScissorsData.Shapes.Paper },
            { RockPaperScissorsData.Shapes.Paper, RockPaperScissorsData.Shapes.Rock }
        };

    private bool Beats(RockPaperScissorsData.Shapes attacker, RockPaperScissorsData.Shapes defender)
    {
        return rpsBeats.ContainsKey(attacker) && rpsBeats[attacker] == defender;
    }

    private float EstimateTttScore(ItemData[] state, GridCell[] cells)
    {
        List<TicTacToeData> marks = new List<TicTacToeData>();
        List<int> markIndices = new List<int>();
        int[] positions = new int[9];

        for (int i = 0; i < 9; i++)
        {
            if (state[i] is TicTacToeData mark)
            {
                marks.Add(mark);
                markIndices.Add(i);
                positions[i] = marks.Count - 1;
            }
            else positions[i] = -1;
        }

        if (marks.Count == 0) return 0f;

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

            if (m0 == -1 || m1 == -1 || m2 == -1) continue;

            if (marks[m0].markType == marks[m1].markType && marks[m1].markType == marks[m2].markType)
                winningLines.Add(line);
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

        float total = 0f;
        for (int i = 0; i < marks.Count; i++)
        {
            float multiplier = Mathf.Pow(1.5f, lineCount[i]);
            int idx = markIndices[i];
            float cellMult = cells[idx].GetMultiplier(marks[i]);
            total += marks[i].score * multiplier * cellMult;
        }

        return total;
    }

    private float EstimateChessScore(ItemData[] state, GridCell[] cells)
    {
        List<ChessData> pieces = new List<ChessData>();
        List<int> indices = new List<int>();

        for (int i = 0; i < 9; i++)
        {
            if (state[i] is ChessData chess)
            {
                pieces.Add(chess);
                indices.Add(i);
            }
        }

        if (pieces.Count == 0) return 0f;

        int[] rows = indices.Select(i => i / 3).ToArray();
        int[] cols = indices.Select(i => i % 3).ToArray();
        List<List<Vector2Int>> attackedByPiece = new List<List<Vector2Int>>();

        for (int i = 0; i < pieces.Count; i++)
            attackedByPiece.Add(GetAttackedCells(pieces[i].TypeOfChessPiece, rows[i], cols[i], rows, cols));

        float total = 0f;
        for (int i = 0; i < pieces.Count; i++)
        {
            int targets = 0;
            for (int j = 0; j < pieces.Count; j++)
            {
                if (i == j) continue;
                Vector2Int pos = new Vector2Int(rows[j], cols[j]);
                if (attackedByPiece[i].Contains(pos)) targets++;
            }

            float multiplier = Mathf.Pow(1.25f, targets);
            int idx = indices[i];
            float cellMult = cells[idx].GetMultiplier(pieces[i]);
            total += pieces[i].score * multiplier * cellMult;
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

        int[][] diagDirs = new int[][] { new int[] { 1, 1 }, new int[] { 1, -1 }, new int[] { -1, 1 }, new int[] { -1, -1 } };
        int[][] rookDirs = new int[][] { new int[] { 0, 1 }, new int[] { 0, -1 }, new int[] { 1, 0 }, new int[] { -1, 0 } };

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
                    for (int dc2 = -1; dc2 <= 1; dc2++)
                    {
                        if (dr2 == 0 && dc2 == 0) continue;
                        int r = row + dr2;
                        int c = col + dc2;
                        if (r >= 0 && r < 3 && c >= 0 && c < 3)
                            attacked.Add(new Vector2Int(r, c));
                    }
                break;
        }

        return attacked;
    }

    private float EstimateCardsScore(ItemData[] state, GridCell[] cells)
    {
        List<CardData> allCards = new List<CardData>();
        List<int> cardIndices = new List<int>();

        for (int i = 0; i < 9; i++)
        {
            if (state[i] is CardData card)
            {
                allCards.Add(card);
                cardIndices.Add(i);
            }
        }

        if (allCards.Count == 0) return 0f;

        float bestMultiplier = 1f;
        HashSet<CardData> bestSet = new HashSet<CardData>();

        if (TryGetFiveOfAKind(allCards, out var fiveSet)) { bestMultiplier = 2f; bestSet = fiveSet; }
        else if (TryGetRoyalFlush(allCards, out var royalSet)) { bestMultiplier = 1.9f; bestSet = royalSet; }
        else if (TryGetStraightFlush(allCards, out var sfSet)) { bestMultiplier = 1.8f; bestSet = sfSet; }
        else if (TryGetFourOfAKind(allCards, out var fourSet)) { bestMultiplier = 1.7f; bestSet = fourSet; }
        else if (TryGetFullHouse(allCards, out var fhSet)) { bestMultiplier = 1.6f; bestSet = fhSet; }
        else if (TryGetFlush(allCards, out var flushSet)) { bestMultiplier = 1.5f; bestSet = flushSet; }
        else if (TryGetStraight(allCards, out var straightSet)) { bestMultiplier = 1.4f; bestSet = straightSet; }
        else if (TryGetThreeOfAKind(allCards, out var threeSet)) { bestMultiplier = 1.3f; bestSet = threeSet; }
        else if (TryGetTwoPair(allCards, out var twoPairSet)) { bestMultiplier = 1.2f; bestSet = twoPairSet; }
        else if (TryGetPair(allCards, out var pairSet)) { bestMultiplier = 1.1f; bestSet = pairSet; }

        float total = 0f;
        for (int i = 0; i < allCards.Count; i++)
        {
            CardData card = allCards[i];
            GridCell cell = cells[cardIndices[i]];
            float cellMult = cell.GetMultiplier(card);
            float comboMult = bestSet.Contains(card) ? bestMultiplier : 1f;
            total += card.score * comboMult * cellMult;
        }

        return total;
    }

    private int Val(CardData.Values v) => (int)v;

    private bool TryGetFiveOfAKind(List<CardData> cards, out HashSet<CardData> bestSet)
    {
        bestSet = null;
        var groups = cards.GroupBy(c => c.value).Where(g => g.Count() >= 5);
        if (!groups.Any()) return false;

        var bestGroup = groups.OrderByDescending(g => Val(g.Key)).First();
        bestSet = new HashSet<CardData>(bestGroup.OrderByDescending(c => c.score).Take(5));
        return true;
    }

    private bool TryGetRoyalFlush(List<CardData> cards, out HashSet<CardData> bestSet)
    {
        bestSet = null;
        var suitGroups = cards.GroupBy(c => c.suit).Where(g => g.Count() >= 5);

        HashSet<CardData> bestRoyal = null;
        int bestSumScore = 0;

        foreach (var suitGroup in suitGroups)
        {
            var royalValues = new[] { CardData.Values.Ten, CardData.Values.Jack, CardData.Values.Queen, CardData.Values.King, CardData.Values.Ace };
            var selected = new List<CardData>();
            bool failed = false;

            foreach (var v in royalValues)
            {
                var card = suitGroup.Where(c => c.value == v).OrderByDescending(c => c.score).FirstOrDefault();
                if (card == null) { failed = true; break; }
                selected.Add(card);
            }

            if (!failed)
            {
                int sum = selected.Sum(c => c.score);
                if (bestRoyal == null || sum > bestSumScore)
                {
                    bestRoyal = new HashSet<CardData>(selected);
                    bestSumScore = sum;
                }
            }
        }

        bestSet = bestRoyal;
        return bestSet != null;
    }

    private bool TryGetStraightFlush(List<CardData> cards, out HashSet<CardData> bestSet)
    {
        bestSet = null;
        var suitGroups = cards.GroupBy(c => c.suit).Where(g => g.Count() >= 5);

        HashSet<CardData> bestSF = null;
        int bestHigh = -1;
        int bestSumScore = 0;

        foreach (var suitGroup in suitGroups)
        {
            var straight = FindBestStraight(suitGroup.ToList());
            if (straight != null)
            {
                int high = straight.Max(c => Val(c.value));
                int sum = straight.Sum(c => c.score);

                if (high > bestHigh || (high == bestHigh && sum > bestSumScore))
                {
                    bestHigh = high;
                    bestSumScore = sum;
                    bestSF = straight;
                }
            }
        }

        bestSet = bestSF;
        return bestSet != null;
    }

    private bool TryGetFourOfAKind(List<CardData> cards, out HashSet<CardData> bestSet)
    {
        bestSet = null;
        var groups = cards.GroupBy(c => c.value).Where(g => g.Count() >= 4);
        if (!groups.Any()) return false;

        var bestGroup = groups.OrderByDescending(g => Val(g.Key)).First();
        bestSet = new HashSet<CardData>(bestGroup.OrderByDescending(c => c.score).Take(4));
        return true;
    }

    private bool TryGetFullHouse(List<CardData> cards, out HashSet<CardData> bestSet)
    {
        bestSet = null;

        var threeGroups = cards.GroupBy(c => c.value).Where(g => g.Count() >= 3).OrderByDescending(g => Val(g.Key));
        var pairGroups = cards.GroupBy(c => c.value).Where(g => g.Count() >= 2).OrderByDescending(g => Val(g.Key));

        foreach (var three in threeGroups)
        {
            var pair = pairGroups.FirstOrDefault(p => p.Key != three.Key);
            if (pair != null)
            {
                var bestThree = three.OrderByDescending(c => c.score).Take(3);
                var bestPair = pair.OrderByDescending(c => c.score).Take(2);

                var set = new HashSet<CardData>(bestThree);
                foreach (var c in bestPair) set.Add(c);

                bestSet = set;
                return true;
            }
        }

        return false;
    }

    private bool TryGetFlush(List<CardData> cards, out HashSet<CardData> bestSet)
    {
        bestSet = null;
        var suitGroups = cards.GroupBy(c => c.suit).Where(g => g.Count() >= 5);
        if (!suitGroups.Any()) return false;

        HashSet<CardData> bestFlush = null;
        int bestHigh = -1;
        int bestSumScore = 0;

        foreach (var group in suitGroups)
        {
            var top5 = group.OrderByDescending(c => Val(c.value)).ThenByDescending(c => c.score).Take(5).ToList();
            int high = Val(top5[0].value);
            int sum = top5.Sum(c => c.score);

            if (high > bestHigh || (high == bestHigh && sum > bestSumScore))
            {
                bestHigh = high;
                bestSumScore = sum;
                bestFlush = new HashSet<CardData>(top5);
            }
        }

        bestSet = bestFlush;
        return true;
    }

    private bool TryGetStraight(List<CardData> cards, out HashSet<CardData> bestSet)
    {
        bestSet = FindBestStraight(cards);
        return bestSet != null;
    }

    private HashSet<CardData> FindBestStraight(List<CardData> cards)
    {
        CardData.Values[] starts =
        {
            CardData.Values.Ten,
            CardData.Values.Nine,
            CardData.Values.Eight,
            CardData.Values.Seven,
            CardData.Values.Six
        };

        HashSet<CardData> best = null;
        int bestHigh = -1;
        int bestSum = 0;

        foreach (var start in starts)
        {
            var needed = new[]
            {
                start,
                (CardData.Values)((int)start + 1),
                (CardData.Values)((int)start + 2),
                (CardData.Values)((int)start + 3),
                (CardData.Values)((int)start + 4),
            };

            var selected = new List<CardData>();
            bool failed = false;

            foreach (var val in needed)
            {
                var card = cards.Where(c => c.value == val).OrderByDescending(c => c.score).FirstOrDefault();
                if (card == null) { failed = true; break; }
                selected.Add(card);
            }

            if (!failed)
            {
                int high = Val(selected.Max(c => c.value));
                int sum = selected.Sum(c => c.score);

                if (high > bestHigh || (high == bestHigh && sum > bestSum))
                {
                    bestHigh = high;
                    bestSum = sum;
                    best = new HashSet<CardData>(selected);
                }
            }
        }

        return best;
    }

    private bool TryGetThreeOfAKind(List<CardData> cards, out HashSet<CardData> bestSet)
    {
        bestSet = null;
        var groups = cards.GroupBy(c => c.value).Where(g => g.Count() >= 3);
        if (!groups.Any()) return false;

        var bestGroup = groups.OrderByDescending(g => Val(g.Key)).First();
        bestSet = new HashSet<CardData>(bestGroup.OrderByDescending(c => c.score).Take(3));
        return true;
    }

    private bool TryGetTwoPair(List<CardData> cards, out HashSet<CardData> bestSet)
    {
        bestSet = null;

        var pairGroups = cards.GroupBy(c => c.value)
            .Where(g => g.Count() >= 2)
            .OrderByDescending(g => Val(g.Key))
            .ToList();

        if (pairGroups.Count < 2) return false;

        var first = pairGroups[0];
        var second = pairGroups[1];

        var bestFirst = first.OrderByDescending(c => c.score).Take(2);
        var bestSecond = second.OrderByDescending(c => c.score).Take(2);

        var set = new HashSet<CardData>(bestFirst);
        foreach (var c in bestSecond) set.Add(c);

        bestSet = set;
        return true;
    }

    private bool TryGetPair(List<CardData> cards, out HashSet<CardData> bestSet)
    {
        bestSet = null;
        var groups = cards.GroupBy(c => c.value).Where(g => g.Count() >= 2);
        if (!groups.Any()) return false;

        var bestGroup = groups.OrderByDescending(g => Val(g.Key)).First();
        bestSet = new HashSet<CardData>(bestGroup.OrderByDescending(c => c.score).Take(2));
        return true;
    }

    private void RefreshHandItemsFromPanel()
    {
        handItems.Clear();
        if (handPanel == null) return;

        foreach (Transform child in handPanel)
        {
            Draggable d = child.GetComponent<Draggable>();
            if (d != null && d.ItemData != null)
                handItems.Add(d);
        }
    }

    private Draggable FindCheapest(List<Draggable> list)
    {
        if (list == null || list.Count == 0) return null;

        Draggable cheapest = null;
        int bestCost = int.MaxValue;

        foreach (var d in list)
        {
            if (d == null || d.ItemData == null) continue;
            int cost = d.ItemData.score;
            if (cost < bestCost)
            {
                bestCost = cost;
                cheapest = d;
            }
        }
        return cheapest;
    }

    private Dictionary<ItemSet, int> CountSetsOnField(ItemData[] state)
    {
        Dictionary<ItemSet, int> dict = new Dictionary<ItemSet, int>();
        for (int i = 0; i < 9; i++)
        {
            ItemData it = state[i];
            if (it == null) continue;

            ItemSet set = GetItemSet(it);
            if (!dict.ContainsKey(set)) dict[set] = 0;
            dict[set]++;
        }
        return dict;
    }

    private ItemSet GetItemSet(ItemData item)
    {
        if (item == null) return ItemSet.Dice;

        if (item is DiceData) return ItemSet.Dice;
        if (item is CardData) return ItemSet.Card;
        if (item is ChessData) return ItemSet.Chess;
        if (item is RockPaperScissorsData) return ItemSet.RockPaperScissors;
        if (item is TicTacToeData) return ItemSet.TicTacToe;

        string tn = item.GetType().Name;
        if (tn.Contains("Dice")) return ItemSet.Dice;
        if (tn.Contains("Card")) return ItemSet.Card;
        if (tn.Contains("Chess")) return ItemSet.Chess;
        if (tn.Contains("RockPaperScissors") || tn.Contains("RPS")) return ItemSet.RockPaperScissors;
        if (tn.Contains("TicTacToe") || tn.Contains("TTT")) return ItemSet.TicTacToe;

        return ItemSet.Dice;
    }
}