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
        List<Draggable> diceDraggables = new List<Draggable>();
        ItemData[] gridState = gridManager.GetGridState();
        GridCell[] cells = gridManager.GetCells();

        for (int i = 0; i < gridState.Length; i++)
        {
            if (gridState[i] is DiceData dice)
            {
                diceList.Add(dice);
                diceIndices.Add(i);
                diceDraggables.Add(cells[i].currentItem);
            }
        }

        yield return new WaitForSeconds(animationDuration);

        List<int> rolledValues = new List<int>();
        for (int i = 0; i < diceList.Count; i++)
        {
            int roll = Random.Range(1, (int)(diceList[i].numberOfFaces) + 1);
            rolledValues.Add(roll);
        }

        // –∑–∞–ø—É—Å–∫–∞–µ–º –¥–ª—è –≤—Å–µ—Ö –∫—É–±–∏–∫–æ–≤ –∞–Ω–∏–º–∞—Ü–∏—é –±—Ä–æ—Å–∫–∞ –æ–¥–Ω–æ–≤—Ä–µ–º–µ–Ω–Ω–æ
        for (int i = 0; i < diceList.Count; i++)
        {
            if (diceDraggables[i] != null)
            {
                int faces = (int)diceList[i].numberOfFaces;
                Draggable d = diceDraggables[i];
                d.StartCoroutine(d.PlayRollAnimation(faces, rolledValues[i]));
            }
        }

        // –∂–¥—ë–º –∑–∞–≤–µ—Ä—à–µ–Ω–∏—è –∞–Ω–∏–º–∞—Ü–∏–∏ + –Ω–µ–±–æ–ª—å—à—É—é –ø–∞—É–∑—É –ø–µ—Ä–µ–¥ –ø–æ–¥—Å—á—ë—Ç–æ–º –æ—á–∫–æ–≤
        yield return new WaitForSeconds(diceList.Count > 0 ? 1.2f : 0.6f);

        float totalScore = CalculateScore(diceList, rolledValues, diceIndices, diceDraggables);

        if (HasAbility("–ë–∞–∑–æ–≤–æ–µ —É—Å–∏–ª–µ–Ω–∏–µ (–ö—É–±–∏–∫–∏)"))
            totalScore *= 1.5f;

        LastScore = totalScore;
    }

    private float CalculateScore(List<DiceData> diceList, List<int> values, List<int> indices, List<Draggable> draggables)
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
            float pieceScore = dice.score * multiplier * cellMult;

            // ÕÓ‚˚Â ÒÔÓÒÓ·ÌÓÒÚË ‰Îˇ ÍÛ·ËÍÓ‚
            if (HasAbility("◊∏ÚÌÓÂ ÛÒËÎÂÌËÂ") && value % 2 == 0)
                pieceScore *= 1.25f;
            if (HasAbility("ÕÂ˜∏ÚÌÓÂ ÛÒËÎÂÌËÂ") && value % 2 != 0)
                pieceScore *= 1.25f;
            if (HasAbility(" ‡ÚÌÓÂ Ú∏Ï ÛÒËÎÂÌËÂ") && value % 3 == 0)
                pieceScore *= 1.5f;
            if (HasAbility(" ‡ÚÌÓÂ ˜ÂÚ˚∏Ï ÛÒËÎÂÌËÂ") && value % 4 == 0)
                pieceScore *= 2f;

            total += pieceScore;

            if (draggables[i] != null)
                draggables[i].ShowScoreGain(Mathf.RoundToInt(pieceScore));
        }

        return total;
    }
}
