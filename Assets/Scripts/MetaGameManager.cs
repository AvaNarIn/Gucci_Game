using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MetaGameManager : MonoBehaviour
{
    public static MetaGameManager Instance { get; private set; }

    public ItemDatabase itemDatabase;
    public AbilityDatabase abilityDatabase;
    public TemporaryBuffDatabase buffDatabase;
    public BossAbilityDatabase bossAbilityDatabase;
    public DeckManager playerDeckManager;
    public DeckManager botDeckManager;
    public TurnManager turnManager;
    public EnemySelectionUI enemySelectionUI;
    public ItemRewardUI itemRewardUI;
    public CellRewardUI cellRewardUI;
    public AbilityRewardUI abilityRewardUI;
    public TemporaryBuffRewardUI tempBuffRewardUI;
    public DeckViewUI deckViewUI;
    public AbilitySlotsUI abilitySlotsUI;
    public Button openDeckButton;
    public Text deckCountText;

    public GameObject levelCompletePanel;
    public Button continueButton;
    public Button exitButton;

    private EnemyInfo[] currentEnemies;
    private EnemyInfo chosenEnemy;
    private List<EnemyInfo.RewardType> effectiveRewards;
    private int rewardIndex;
    private int enemiesDefeated;
    private int currentEnemyHealth = 30;

    public GameObject settingsPanel;   // панель с кнопкой "Выйти в меню"
    public Button exitToMenuButton;    // кнопка внутри settingsPanel

    // Таблица здоровья для 18 боссов (6 уровней * 3 босса)
    private int[] enemyHealthTable = new int[]
    {
        30, 40, 60, 80, 110, 170, 220, 280, 430, 540,
        680, 1100, 1400, 1800, 2800, 3500, 4400, 6700
    };

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        itemDatabase.Init();
        abilityDatabase.Init();
        turnManager.OnGameOver += OnBattleFinished;

        exitToMenuButton.onClick.AddListener(ExitToMenu);

        if (PlayerInventory.cards.Count == 0)
            PlayerInventory.cards.AddRange(playerDeckManager.GetDeck());

        deckViewUI.playerDeckManager = playerDeckManager;
        deckViewUI.playerGridManager = turnManager.PlayerGridManager;
        itemRewardUI.Init(deckViewUI, playerDeckManager);
        abilityRewardUI.Init(abilitySlotsUI);
        abilitySlotsUI.abilityDatabase = abilityDatabase;

        RefreshDeckButtonText();
        abilitySlotsUI.UpdateSlots();

        if (!LevelManager.runActive)
        {
            SceneManager.LoadScene("MainMenu");
            return;
        }

        enemiesDefeated = 0;
        currentEnemyHealth = 30;
        GenerateEnemies();
    }

    public void RefreshDeckButtonText()
    {
        if (deckCountText != null && playerDeckManager != null)
            deckCountText.text = $"Колода ({GameUtils.FormatNumber(playerDeckManager.GetDrawPile().Count)})";
    }

    public void OpenDeck()
    {
        deckViewUI.Show();
    }

    public void ExitToMenu()
    {
        LevelManager.EndRun();
        SceneManager.LoadScene("MainMenu");
    }

    void GenerateEnemies()
    {
        bool isBoss = (enemiesDefeated + 1) % 3 == 0;
        int count = isBoss ? 3 : 2;
        currentEnemies = new EnemyInfo[count];
        for (int i = 0; i < count; i++)
            currentEnemies[i] = GenerateRandomEnemy(isBoss);
        enemySelectionUI.Show(currentEnemies);
    }

    EnemyInfo GenerateRandomEnemy(bool isBoss)
    {
        EnemyInfo enemy = new EnemyInfo();
        enemy.isBoss = isBoss;
        var sets = System.Enum.GetValues(typeof(ItemSet)) as ItemSet[];

        if (isBoss)
        {
            BossAbilityData bossAbility = bossAbilityDatabase.GetRandomAbility();
            enemy.abilityDescription = bossAbility != null ? bossAbility.abilityName : "Неизвестная способность";
            enemy.set1 = sets[Random.Range(0, sets.Length)];
            enemy.set2 = enemy.set1;
            enemy.rewards = new EnemyInfo.RewardType[3];
        }
        else
        {
            enemy.abilityDescription = "";
            enemy.set1 = sets[Random.Range(0, sets.Length)];
            do { enemy.set2 = sets[Random.Range(0, sets.Length)]; }
            while (enemy.set2 == enemy.set1);
            enemy.rewards = new EnemyInfo.RewardType[2];
        }

        int healthIndex = enemiesDefeated;
        if (healthIndex < enemyHealthTable.Length)
        {
            enemy.health = enemyHealthTable[healthIndex];
        }
        else
        {
            float lastHealth = enemyHealthTable[enemyHealthTable.Length - 1];
            int battlesAfterTable = healthIndex - enemyHealthTable.Length + 1;
            float multiplier = isBoss ? Mathf.Pow(2f, battlesAfterTable) : Mathf.Pow(1.5f, battlesAfterTable);
            enemy.health = GameUtils.RoundToPretty(lastHealth * multiplier);
        }

        for (int i = 0; i < enemy.rewards.Length; i++)
        {
            int rand = Random.Range(0, 4);
            enemy.rewards[i] = rand == 3 ? EnemyInfo.RewardType.Random : (EnemyInfo.RewardType)rand;
        }

        return enemy;
    }

    void DistributeHealthToCells(int totalHealth, GridCell[] cells)
    {
        int numCells = cells.Length;
        float minRatio = 0.05f;
        float maxRatio = 0.15f;
        float totalRatio = 0.90f;

        float[] ratios = new float[numCells];
        for (int i = 0; i < numCells; i++)
            ratios[i] = minRatio;

        float remainingRatio = totalRatio - minRatio * numCells;

        System.Random rng = new System.Random();
        for (int i = 0; i < numCells && remainingRatio > 0.0001f; i++)
        {
            float maxAdditional = maxRatio - minRatio;
            float add = (float)rng.NextDouble() * maxAdditional;
            if (add > remainingRatio) add = remainingRatio;
            ratios[i] += add;
            remainingRatio -= add;
        }

        if (remainingRatio > 0f)
            ratios[0] += remainingRatio;

        for (int i = 0; i < numCells; i++)
        {
            int cellHealth = Mathf.RoundToInt(ratios[i] * totalHealth);
            if (cellHealth < 1) cellHealth = 1;
            cells[i].SetProperties(cellHealth, cells[i].cellType, cells[i].multiplier);
        }
    }

    void SetupEnemyCellTypes(EnemyInfo enemy, GridCell[] cells)
    {

        foreach (var cell in cells)
        {
            cell.SetProperties(cell.CurrentHealth, CellType.Empty, 1f);
        }

        int specialCellsCount = enemy.isBoss ? 3 : 2;
        float targetSum = enemy.isBoss ? 4.5f : 3.0f;

        List<int> indices = new List<int>();
        for (int i = 0; i < cells.Length; i++) indices.Add(i);
        Shuffle(indices);
        List<int> specialIndices = indices.GetRange(0, specialCellsCount);

        float[] multipliers = GenerateMultipliers(specialCellsCount, targetSum);

        CellType primaryType = ItemSetToCellType(enemy.set1);
        CellType secondaryType = enemy.isBoss ? primaryType : ItemSetToCellType(enemy.set2);

        for (int i = 0; i < specialCellsCount; i++)
        {
            int cellIndex = specialIndices[i];
            CellType type = enemy.isBoss ? primaryType : (i == 0 ? primaryType : secondaryType);
            cells[cellIndex].SetProperties(cells[cellIndex].CurrentHealth, type, multipliers[i]);
        }
    }

    void Shuffle<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    float[] GenerateMultipliers(int count, float targetSum)
    {
        float[] multipliers = new float[count];
        float min = 1.0f;
        float max = 2.0f;
        float step = 0.1f;

        if (count == 2 && Mathf.Abs(targetSum - 2.0f) < 0.001f)
        {
            multipliers[0] = 1.0f;
            multipliers[1] = 1.0f;
            return multipliers;
        }

        System.Random rng = new System.Random();
        float remaining = targetSum;
        for (int i = 0; i < count - 1; i++)
        {
            float maxForThis = Mathf.Min(max, remaining - (count - 1 - i) * min);
            float minForThis = Mathf.Max(min, remaining - (count - 1 - i) * max);
            int steps = Mathf.RoundToInt((maxForThis - minForThis) / step);
            float chosen;
            if (steps <= 0)
                chosen = minForThis;
            else
            {
                int randomStep = rng.Next(steps + 1);
                chosen = minForThis + randomStep * step;
            }
            multipliers[i] = chosen;
            remaining -= chosen;
        }
        multipliers[count - 1] = remaining;

        for (int i = 0; i < count; i++)
            multipliers[i] = Mathf.Round(multipliers[i] * 10f) / 10f;

        return multipliers;
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

    public void OnEnemySelected(int index)
    {
        chosenEnemy = currentEnemies[index];
        ResetBattleState();

        List<ItemData> botDeck = new List<ItemData>();
        for (int i = 0; i < 20; i++) botDeck.Add(itemDatabase.GetRandomItem(chosenEnemy.set1));
        for (int i = 0; i < 20; i++) botDeck.Add(itemDatabase.GetRandomItem(chosenEnemy.set2));
        for (int i = 0; i < 5; i++)
            botDeck.Add(itemDatabase.GetRandomItemExcluding(chosenEnemy.set1, chosenEnemy.set2));

        Debug.Log($"[MetaGameManager] Сформирована колода бота: {botDeck.Count} карт");
        for (int i = 0; i < botDeck.Count; i++)
        {
            if (botDeck[i] == null)
                Debug.LogError($"[MetaGameManager] В botDeck[{i}] находится null!");
            else
                Debug.Log($"[MetaGameManager] Карта {i}: {botDeck[i].displayName}");
        }

        botDeckManager.SetCustomDeck(botDeck);

        playerDeckManager.SetCustomDeck(PlayerInventory.cards);
        RefreshDeckButtonText();

        turnManager.botCharacter.SetMaxHealth(chosenEnemy.health);
        turnManager.botCharacter.ResetHealth();

        DistributeHealthToCells(chosenEnemy.health, turnManager.BotGridManager.GetCells());
        SetupEnemyCellTypes(chosenEnemy, turnManager.BotGridManager.GetCells());

        BotAbilityHandler botAbilityHandler = turnManager.BotGridManager.GetComponent<BotAbilityHandler>();
        if (botAbilityHandler != null)
        {
            BossAbilityData bossAbility = System.Array.Find(bossAbilityDatabase.allAbilities, a => a.abilityName == chosenEnemy.abilityDescription);
            botAbilityHandler.SetBossAbility(bossAbility);
            turnManager.SetBotAbilityHandler(botAbilityHandler);
        }

        enemySelectionUI.gameObject.SetActive(false);
        turnManager.StartBattle();
    }

    void ResetBattleState()
    {
        foreach (Transform child in playerDeckManager.handPanel)
            Destroy(child.gameObject);
        foreach (Transform child in botDeckManager.handPanel)
            Destroy(child.gameObject);

        foreach (var cell in turnManager.PlayerGridManager.GetCells())
        {
            if (cell.currentItem != null)
                cell.RemoveItem();
            cell.ResetHealth();
        }
        foreach (var cell in turnManager.BotGridManager.GetCells())
        {
            if (cell.currentItem != null)
                cell.RemoveItem();
            cell.ResetHealth();
        }

        turnManager.playerCharacter.ResetHealth();
        turnManager.botCharacter.ResetHealth();
    }

    void OnBattleFinished(bool playerWon)
    {
        ResetBattleState();
        PlayerInventory.DecreaseBuffDurations();

        if (turnManager != null)
            turnManager.UpdateBuffDisplay();

        if (!playerWon)
        {
            LevelManager.EndRun();
            SceneManager.LoadScene("MainMenu");
            return;
        }

        enemiesDefeated++;
        currentEnemyHealth = chosenEnemy.health;

        if (chosenEnemy.isBoss)
        {
            LevelManager.OnBossDefeated();
        }

        if (LevelManager.levelCompleted)
        {
            ProgressManager.SetMaxLevel(LevelManager.selectedCharacter, LevelManager.currentLevel);
            levelCompletePanel.SetActive(true);
            return;
        }

        playerDeckManager.SetCustomDeck(PlayerInventory.cards);
        RefreshDeckButtonText();

        if (LevelManager.levelCompleted)
        {
            levelCompletePanel.SetActive(true);
            return;
        }

        effectiveRewards = new List<EnemyInfo.RewardType>();
        foreach (var r in chosenEnemy.rewards)
        {
            if (r == EnemyInfo.RewardType.Random)
                effectiveRewards.Add((EnemyInfo.RewardType)Random.Range(0, 4));
            else
                effectiveRewards.Add(r);
        }

        rewardIndex = 0;
        ShowNextReward();
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void OnContinueClicked()
    {
        levelCompletePanel.SetActive(false);
        LevelManager.levelCompleted = false;
        GenerateEnemies();
    }

    public void OnExitClicked()
    {
        LevelManager.EndRun();
        SceneManager.LoadScene("MainMenu");
    }

    void ShowNextReward()
    {
        if (rewardIndex >= effectiveRewards.Count)
        {
            GenerateEnemies();
            return;
        }

        var rewardType = effectiveRewards[rewardIndex];
        switch (rewardType)
        {
            case EnemyInfo.RewardType.Item:
                itemRewardUI.gameObject.SetActive(true);
                itemRewardUI.Offer(chosenEnemy.set1, chosenEnemy.set2, itemDatabase, () =>
                {
                    itemRewardUI.gameObject.SetActive(false);
                    rewardIndex++;
                    ShowNextReward();
                });
                break;
            case EnemyInfo.RewardType.Cell:
                cellRewardUI.gameObject.SetActive(true);
                cellRewardUI.Offer(chosenEnemy.set1, chosenEnemy.set2, () =>
                {
                    cellRewardUI.gameObject.SetActive(false);
                    rewardIndex++;
                    ShowNextReward();
                });
                break;
            case EnemyInfo.RewardType.Ability:
                abilityRewardUI.gameObject.SetActive(true);
                abilityRewardUI.Offer(chosenEnemy.set1, chosenEnemy.set2, abilityDatabase, () =>
                {
                    abilityRewardUI.gameObject.SetActive(false);
                    rewardIndex++;
                    ShowNextReward();
                });
                break;
            case EnemyInfo.RewardType.TemporaryBuff:
                tempBuffRewardUI.gameObject.SetActive(true);
                tempBuffRewardUI.Offer(buffDatabase, () =>
                {
                    tempBuffRewardUI.gameObject.SetActive(false);
                    rewardIndex++;
                    ShowNextReward();
                });
                break;
        }
    }
}