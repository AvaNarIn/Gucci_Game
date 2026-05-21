using System;
using UnityEngine;
using UnityEngine.UI;

public class CellRewardUI : MonoBehaviour
{
    [Serializable]
    private struct CellReward
    {
        public int position;
        public int maxHealth;
        public CellType type;
        public float multiplier;
    }

    public Button[] choiceButtons;
    public Button skipButton;
    public GridManager playerGridManager;

    private CellReward[] offeredCells;
    private Action onComplete;

    void Start()
    {
        skipButton.onClick.AddListener(() => {
            GiveRandomBuffAndClose();
        });
        // gameObject.SetActive(false); убран
    }

    public void Offer(ItemSet set1, ItemSet set2, Action onFinished)
    {
        onComplete = onFinished;
        gameObject.SetActive(true);
        offeredCells = new CellReward[3];
        for (int i = 0; i < 3; i++)
        {
            CellReward cell = new CellReward();
            cell.position = UnityEngine.Random.Range(0, 9);
            cell.maxHealth = UnityEngine.Random.Range(5, 20);
            cell.type = i == 0 ? ItemSetToCellType(set1) :
                        i == 1 ? ItemSetToCellType(set2) :
                        GetRandomCellTypeExcluding(set1, set2);
            cell.multiplier = UnityEngine.Random.Range(1.1f, 3.0f);
            offeredCells[i] = cell;
            choiceButtons[i].GetComponentInChildren<Text>().text = $"Pos: {cell.position}, HP: {cell.maxHealth}, Type: {cell.type}, Mult: {cell.multiplier:F2}";
            int idx = i;
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => OnCellChosen(idx));
        }
    }

    CellType ItemSetToCellType(ItemSet set) => set switch
    {
        ItemSet.Dice => CellType.Dice,
        ItemSet.Card => CellType.Card,
        ItemSet.Chess => CellType.Chess,
        ItemSet.RockPaperScissors => CellType.RockPaperScissors,
        ItemSet.TicTacToe => CellType.TicTacToe,
        _ => CellType.Empty
    };

    CellType GetRandomCellTypeExcluding(ItemSet ex1, ItemSet ex2)
    {
        var values = System.Enum.GetValues(typeof(CellType));
        CellType t;
        do
        {
            t = (CellType)values.GetValue(UnityEngine.Random.Range(1, values.Length));
        } while (t == ItemSetToCellType(ex1) || t == ItemSetToCellType(ex2));
        return t;
    }

    void OnCellChosen(int idx)
    {
        GridCell cell = playerGridManager.GetCells()[offeredCells[idx].position];
        cell.SetProperties(offeredCells[idx].maxHealth, offeredCells[idx].type, offeredCells[idx].multiplier);
        Close();
    }

    void GiveRandomBuffAndClose()
    {
        TemporaryBuffDatabase buffDB = MetaGameManager.Instance.buffDatabase;
        if (buffDB != null && buffDB.allBuffs.Count > 0)
        {
            TemporaryBuffData randomBuff = buffDB.GetRandomBuff();
            if (randomBuff != null)
            {
                int duration = UnityEngine.Random.Range(1, 6);
                PlayerInventory.activeBuffs.Add(new BuffInstance(randomBuff, duration));
                if (TurnManager.Instance != null)
                    TurnManager.Instance.UpdateBuffDisplay();
            }
        }
        Close();
    }

    void Close()
    {
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }
}