using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardHandler : ItemHandler
{
    private int Val(CardData.Values v) => (int)v;

    public override IEnumerator ApplyingEffects_Coroutine()
    {
        yield return new WaitForSeconds(animationDuration);
    }

    public override IEnumerator CountingScore_Coroutine()
    {
        ItemData[] gridState = gridManager.GetGridState();
        GridCell[] cells = gridManager.GetCells();
        List<CardData> allCards = new List<CardData>();
        List<int> cardIndices = new List<int>();
        List<Draggable> cardDraggables = new List<Draggable>();

        for (int i = 0; i < gridState.Length; i++)
        {
            if (gridState[i] is CardData card)
            {
                allCards.Add(card);
                cardIndices.Add(i);
                cardDraggables.Add(cells[i].currentItem);
            }
        }

        yield return new WaitForSeconds(animationDuration);

        // === Обработка способности "Усиление комбинации" ===
        string abilityName = "Усиление комбинации";
        AbilityData boostAbility = GetAbilityByName(abilityName);
        string storedCombo = null;

        if (boostAbility != null && ActiveAbilities[boostAbility])
        {
            storedCombo = GetAbilityState(abilityName) as string;
            if (string.IsNullOrEmpty(storedCombo))
            {
                string[] combos = { "Пара", "Две пары", "Сет", "Стрит", "Флеш", "Фулл-хаус", "Каре", "Стрит-флеш", "Флеш-рояль", "Пять одинаковых" };
                storedCombo = combos[Random.Range(0, combos.Length)];
                SetAbilityState(abilityName, storedCombo);
            }
        }
        else if (boostAbility != null && !ActiveAbilities[boostAbility])
        {
            RemoveAbilityState(abilityName);
        }
        // Если boostAbility == null – такой способности вообще нет в базе, ничего не делаем

        // Подсчёт очков (передаём storedCombo)
        float totalScore = CalculateScore(allCards, cardIndices, cardDraggables, storedCombo);
        LastScore = totalScore;
        Debug.Log($"Очки за карты: {totalScore}");
    }

    private float CalculateScore(List<CardData> cards, List<int> cardIndices, List<Draggable> draggables, string storedCombo)
    {
        if (cards.Count == 0) return 0;

        float bestMultiplier = 1f;
        HashSet<CardData> bestSet = new HashSet<CardData>();

        if (TryGetFiveOfAKind(cards, out var fiveSet))
        { bestMultiplier = 2f; bestSet = fiveSet; }
        else if (TryGetRoyalFlush(cards, out var royalSet))
        { bestMultiplier = 1.9f; bestSet = royalSet; }
        else if (TryGetStraightFlush(cards, out var sfSet))
        { bestMultiplier = 1.8f; bestSet = sfSet; }
        else if (TryGetFourOfAKind(cards, out var fourSet))
        { bestMultiplier = 1.7f; bestSet = fourSet; }
        else if (TryGetFullHouse(cards, out var fhSet))
        { bestMultiplier = 1.6f; bestSet = fhSet; }
        else if (TryGetFlush(cards, out var flushSet))
        { bestMultiplier = 1.5f; bestSet = flushSet; }
        else if (TryGetStraight(cards, out var straightSet))
        { bestMultiplier = 1.4f; bestSet = straightSet; }
        else if (TryGetThreeOfAKind(cards, out var threeSet))
        { bestMultiplier = 1.3f; bestSet = threeSet; }
        else if (TryGetTwoPair(cards, out var twoPairSet))
        { bestMultiplier = 1.2f; bestSet = twoPairSet; }
        else if (TryGetPair(cards, out var pairSet))
        { bestMultiplier = 1.1f; bestSet = pairSet; }

        float effectiveMultiplier = bestMultiplier;
        if (!string.IsNullOrEmpty(storedCombo))
        {
            string currentCombo = GetCurrentComboName(bestSet, cards);
            if (currentCombo == storedCombo)
                effectiveMultiplier *= 2f;
        }

        float total = 0f;
        GridCell[] cells = gridManager.GetCells();
        for (int i = 0; i < cards.Count; i++)
        {
            CardData card = cards[i];
            GridCell cell = cells[cardIndices[i]];
            float cellMult = cell.GetMultiplier(card);
            float comboMult = bestSet.Contains(card) ? effectiveMultiplier : 1f;
            float pieceScore = card.score * comboMult * cellMult;
            total += pieceScore;

            if (draggables[i] != null)
                draggables[i].ShowScoreGain(Mathf.RoundToInt(pieceScore));
        }

        return total;
    }

    private string GetCurrentComboName(HashSet<CardData> bestSet, List<CardData> cards)
    {
        if (TryGetFiveOfAKind(cards, out var fiveSet) && fiveSet.SetEquals(bestSet)) return "Пять одинаковых";
        if (TryGetRoyalFlush(cards, out var royalSet) && royalSet.SetEquals(bestSet)) return "Флеш-рояль";
        if (TryGetStraightFlush(cards, out var sfSet) && sfSet.SetEquals(bestSet)) return "Стрит-флеш";
        if (TryGetFourOfAKind(cards, out var fourSet) && fourSet.SetEquals(bestSet)) return "Каре";
        if (TryGetFullHouse(cards, out var fhSet) && fhSet.SetEquals(bestSet)) return "Фулл-хаус";
        if (TryGetFlush(cards, out var flushSet) && flushSet.SetEquals(bestSet)) return "Флеш";
        if (TryGetStraight(cards, out var straightSet) && straightSet.SetEquals(bestSet)) return "Стрит";
        if (TryGetThreeOfAKind(cards, out var threeSet) && threeSet.SetEquals(bestSet)) return "Сет";
        if (TryGetTwoPair(cards, out var twoPairSet) && twoPairSet.SetEquals(bestSet)) return "Две пары";
        if (TryGetPair(cards, out var pairSet) && pairSet.SetEquals(bestSet)) return "Пара";
        return "Старшая карта";
    }

    private bool TryGetFiveOfAKind(List<CardData> cards, out HashSet<CardData> bestSet)
    {
        bestSet = null;
        var groups = cards.GroupBy(c => c.value).Where(g => g.Count() >= 5);
        if (!groups.Any()) return false;

        var bestGroup = groups.OrderByDescending(g => Val(g.Key)).First();
        var top5 = bestGroup.OrderByDescending(c => c.score).Take(5);
        bestSet = new HashSet<CardData>(top5);
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
            var royalValues = new[] { CardData.Values.Ten, CardData.Values.Jack,
                                      CardData.Values.Queen, CardData.Values.King,
                                      CardData.Values.Ace };
            var selected = new List<CardData>();
            bool failed = false;
            foreach (var v in royalValues)
            {
                var card = suitGroup.Where(c => c.value == v)
                                    .OrderByDescending(c => c.score).FirstOrDefault();
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
        var top4 = bestGroup.OrderByDescending(c => c.score).Take(4);
        bestSet = new HashSet<CardData>(top4);
        return true;
    }

    private bool TryGetFullHouse(List<CardData> cards, out HashSet<CardData> bestSet)
    {
        bestSet = null;
        var threeGroups = cards.GroupBy(c => c.value)
                               .Where(g => g.Count() >= 3)
                               .OrderByDescending(g => Val(g.Key));
        var pairGroups = cards.GroupBy(c => c.value)
                              .Where(g => g.Count() >= 2)
                              .OrderByDescending(g => Val(g.Key));

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
            var top5 = group.OrderByDescending(c => Val(c.value))
                           .ThenByDescending(c => c.score)
                           .Take(5).ToList();
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

        CardData.Values[] starts = {
            CardData.Values.Ten,   // 10–Ace
            CardData.Values.Nine,  // 9–King
            CardData.Values.Eight, // 8–Queen
            CardData.Values.Seven, // 7–Jack
            CardData.Values.Six    // 6–10
        };

        HashSet<CardData> best = null;
        int bestHigh = -1;
        int bestSum = 0;

        foreach (var start in starts)
        {
            CardData.Values[] needed = {
                start, start + 1, start + 2, start + 3, start + 4
            };
            var selected = new List<CardData>();
            bool failed = false;
            foreach (var val in needed)
            {
                var card = cards.Where(c => c.value == val)
                                .OrderByDescending(c => c.score).FirstOrDefault();
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
        var top3 = bestGroup.OrderByDescending(c => c.score).Take(3);
        bestSet = new HashSet<CardData>(top3);
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
        var top2 = bestGroup.OrderByDescending(c => c.score).Take(2);
        bestSet = new HashSet<CardData>(top2);
        return true;
    }
}