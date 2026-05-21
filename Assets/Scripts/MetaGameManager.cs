using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MetaGameManager : MonoBehaviour
{
    public static MetaGameManager Instance { get; private set; }

    public ItemDatabase itemDatabase;
    public AbilityDatabase abilityDatabase;
    public TemporaryBuffDatabase buffDatabase;
    public BossAbilityDatabase bossAbilityDatabase;   // новая база способностей босса
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

    private EnemyInfo[] currentEnemies;
    private EnemyInfo chosenEnemy;
    private List<EnemyInfo.RewardType> effectiveRewards;
    private int rewardIndex;
    private int enemiesDefeated;
    private int nextEnemyHealth = 5;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        itemDatabase.Init();
        abilityDatabase.Init();
        turnManager.OnGameOver += OnBattleFinished;

        if (PlayerInventory.cards.Count == 0)
            PlayerInventory.cards.AddRange(playerDeckManager.GetDeck());

        deckViewUI.playerDeckManager = playerDeckManager;
        deckViewUI.playerGridManager = turnManager.PlayerGridManager;
        itemRewardUI.Init(deckViewUI, playerDeckManager);
        abilityRewardUI.Init(abilitySlotsUI);
        abilitySlotsUI.abilityDatabase = abilityDatabase;

        RefreshDeckButtonText();
        abilitySlotsUI.UpdateSlots();
        enemiesDefeated = 0;
        GenerateEnemies();
    }

    public void RefreshDeckButtonText()
    {
        if (deckCountText != null && playerDeckManager != null)
            deckCountText.text = $"Колода ({playerDeckManager.GetDrawPile().Count})";
    }

    public void OpenDeck()
    {
        deckViewUI.Show();
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
            // Выбираем случайную способность из базы боссов
            BossAbilityData bossAbility = bossAbilityDatabase.GetRandomAbility();
            enemy.abilityDescription = bossAbility != null ? bossAbility.abilityName : "Неизвестная способность";

            enemy.set1 = sets[Random.Range(0, sets.Length)];
            enemy.set2 = enemy.set1;
            enemy.rewards = new EnemyInfo.RewardType[3];
            enemy.health = Mathf.FloorToInt(nextEnemyHealth * 1.5f);
        }
        else
        {
            enemy.abilityDescription = "";
            enemy.set1 = sets[Random.Range(0, sets.Length)];
            do { enemy.set2 = sets[Random.Range(0, sets.Length)]; }
            while (enemy.set2 == enemy.set1);
            enemy.rewards = new EnemyInfo.RewardType[2];
            enemy.health = Mathf.FloorToInt(nextEnemyHealth * 1.25f);
        }

        for (int i = 0; i < enemy.rewards.Length; i++)
        {
            int rand = Random.Range(0, 4);
            enemy.rewards[i] = rand == 3 ? EnemyInfo.RewardType.Random : (EnemyInfo.RewardType)rand;
        }

        return enemy;
    }

    public void OnEnemySelected(int index)
    {
        chosenEnemy = currentEnemies[index];
        ResetBattleState();

        List<ItemData> botDeck = new List<ItemData>();
        for (int i = 0; i < 20; i++) botDeck.Add(itemDatabase.GetRandomItem(chosenEnemy.set1));
        for (int i = 0; i < 20; i++) botDeck.Add(itemDatabase.GetRandomItem(chosenEnemy.set2));
        for (int i = 0; i < 5; i++)
            botDeck.Add(itemDatabase.GetRandomItemExcluding(chosenEnemy.set1, chosenEnemy.set2));
        botDeckManager.SetCustomDeck(botDeck);

        playerDeckManager.SetCustomDeck(PlayerInventory.cards);
        RefreshDeckButtonText();

        turnManager.botCharacter.SetMaxHealth(chosenEnemy.health);
        turnManager.botCharacter.ResetHealth();

        // Передаём способность босса обработчику
        BotAbilityHandler botAbilityHandler = turnManager.botGridManager.GetComponent<BotAbilityHandler>();
        if (botAbilityHandler != null)
        {
            BossAbilityData bossAbility = System.Array.Find(bossAbilityDatabase.allAbilities, a => a.abilityName == chosenEnemy.abilityDescription);
            botAbilityHandler.SetBossAbility(bossAbility);
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

        // Обновляем текст баффов сразу после боя
        if (turnManager != null)
            turnManager.UpdateBuffDisplay();

        if (!playerWon)
        {
            GenerateEnemies();
            return;
        }

        enemiesDefeated++;
        nextEnemyHealth = chosenEnemy.health;

        playerDeckManager.SetCustomDeck(PlayerInventory.cards);
        RefreshDeckButtonText();

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