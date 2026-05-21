using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TurnPhase
{
    PlayerPlacement,
    PlayerAction,
    BotPlacement,
    BotAction
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [SerializeField] private GridManager playerGridManager;
    public GridManager botGridManager;
    [SerializeField] private BotController botController;
    [SerializeField] private Button actionButton;
    [SerializeField] private Text actionButtonText;
    [SerializeField] private Text playerScoreText;
    [SerializeField] private Text botScoreText;
    [SerializeField] private Text playerManaText;
    [SerializeField] private Text botManaText;
    [SerializeField] private Transform playerHandPanel;
    [SerializeField] private Transform botHandPanel;
    public Character playerCharacter;
    public Character botCharacter;
    [SerializeField] private DeckManager playerDeckManager;
    [SerializeField] private DeckManager botDeckManager;
    [SerializeField] private Text buffsText;

    // Добавленное поле
    private BotAbilityHandler botAbilityHandler;

    public GridManager PlayerGridManager => playerGridManager;
    public GridManager BotGridManager => botGridManager;

    public System.Action<bool> OnGameOver;

    private TurnPhase currentPhase;
    private int playerAccumulatedScore;
    private int botAccumulatedScore;
    private int playerMana;
    private int botMana;
    private int roundNumber;
    private bool firstPlayerTurn = true;
    private bool firstBotTurn = true;
    private List<Draggable> subscribedDraggables = new List<Draggable>();
    private bool gameOverTriggered = false;

    private void Awake()
    {
        Instance = this;
        playerCharacter.OnDeath += () => TriggerGameOver(false);
        botCharacter.OnDeath += () => TriggerGameOver(true);
        botAbilityHandler = botGridManager.GetComponent<BotAbilityHandler>();
    }

    public void StartBattle()
    {
        gameOverTriggered = false;
        playerMana = 8;                     // изменено на 8
        botMana = 8;                        // изменено на 8
        firstPlayerTurn = true;
        firstBotTurn = true;
        playerAccumulatedScore = 0;
        botAccumulatedScore = 0;
        roundNumber = 1;

        int bonusMana = 0;
        foreach (var buff in PlayerInventory.activeBuffs)
        {
            if (buff.data.buffName == "Дополнительная мана")
                bonusMana += 2;
        }
        playerMana += bonusMana;

        playerDeckManager.DrawInitialHand();
        InitializeHand(playerGridManager, playerHandPanel, true);
        InitializeHand(botGridManager, botHandPanel, false);

        foreach (var handler in FindObjectsByType<ItemHandler>())
            handler.RefreshAbilities();

        UpdateBuffDisplay();
        SetPhase(TurnPhase.PlayerPlacement);
        UpdateUI();
    }

    public void SetBotAbilityHandler(BotAbilityHandler handler)
    {
        botAbilityHandler = handler;
    }

    private void InitializeHand(GridManager manager, Transform handPanel, bool isDraggable)
    {
        foreach (Transform child in handPanel)
        {
            Draggable d = child.GetComponent<Draggable>();
            if (d != null)
            {
                d.Initialize(manager);
                d.SetDraggable(isDraggable);
            }
        }
    }

    private void SetPhase(TurnPhase phase)
    {
        UnsubscribeAttackTargets();
        currentPhase = phase;

        bool canDrag = (phase == TurnPhase.PlayerPlacement);
        SetPlayerItemsDraggable(canDrag);

        switch (phase)
        {
            case TurnPhase.PlayerPlacement:
                if (!firstPlayerTurn)
                    AddPlayerMana(5);
                firstPlayerTurn = false;
                // В первом раунде кнопка сразу "Завершить ход"
                actionButtonText.text = (roundNumber == 1) ? "Завершить ход" : "Подсчитать очки";
                actionButton.interactable = true;
                break;
            case TurnPhase.PlayerAction:
                actionButtonText.text = "Завершить ход";
                actionButton.interactable = true;
                SubscribeToEnemyDraggables();
                break;
            case TurnPhase.BotPlacement:
                if (!firstBotTurn)
                    AddBotMana(5);
                firstBotTurn = false;
                actionButton.interactable = false;
                break;
            case TurnPhase.BotAction:
                actionButton.interactable = false;
                break;
        }
        UpdateUI();
    }

    private void SubscribeToEnemyDraggables()
    {
        Draggable[] all = FindObjectsByType<Draggable>();
        foreach (var d in all)
        {
            if (d.OwnerGridManager == botGridManager)
            {
                d.OnClicked += OnEnemyDraggableClicked;
                subscribedDraggables.Add(d);
            }
        }
        if (botCharacter != null)
            botCharacter.OnClicked += OnBotCharacterClicked;
    }

    private void UnsubscribeAttackTargets()
    {
        foreach (var d in subscribedDraggables)
            d.OnClicked -= OnEnemyDraggableClicked;
        subscribedDraggables.Clear();
        if (botCharacter != null)
            botCharacter.OnClicked -= OnBotCharacterClicked;
    }

    private void OnEnemyDraggableClicked(Draggable draggable)
    {
        if (currentPhase != TurnPhase.PlayerAction) return;
        GridCell cell = draggable.GetComponentInParent<GridCell>();
        if (cell == null) return;

        int damage = Mathf.Min(playerAccumulatedScore, cell.CurrentHealth);
        if (damage > 0)
        {
            cell.TakeDamage(damage);
            playerAccumulatedScore -= damage;
            UpdateUI();
        }
    }

    private void OnBotCharacterClicked(Character character)
    {
        if (currentPhase != TurnPhase.PlayerAction) return;
        int damage = Mathf.Min(playerAccumulatedScore, character.CurrentHealth);
        if (damage > 0)
        {
            character.TakeDamage(damage);
            playerAccumulatedScore -= damage;
            UpdateUI();
        }
    }

    private void SetPlayerItemsDraggable(bool draggable)
    {
        Draggable[] all = FindObjectsByType<Draggable>();
        foreach (var d in all)
        {
            if (d.OwnerGridManager == playerGridManager)
                d.SetDraggable(draggable);
        }
    }

    public void OnActionButton()
    {
        switch (currentPhase)
        {
            case TurnPhase.PlayerPlacement:
                if (roundNumber == 1)
                {
                    // Первый раунд: сразу передаём ход боту без фазы PlayerAction
                    StartBotTurn();
                }
                else
                {
                    StartCoroutine(CalculatePlayerScore());
                }
                break;
            case TurnPhase.PlayerAction:
                StartBotTurn();
                break;
        }
    }

    private IEnumerator CalculatePlayerScore()
    {
        int score = 0;
        yield return StartCoroutine(playerGridManager.CountingCoroutine(value => score = value));
        playerAccumulatedScore += score;
        UpdateUI();
        SetPhase(TurnPhase.PlayerAction);
    }

    private void StartBotTurn()
    {
        if (firstBotTurn)
            botDeckManager.DrawTurnCards(3);
        else
            botDeckManager.DrawTurnCards(2);

        CheckEndGame();
        SetPhase(TurnPhase.BotPlacement);
        botController.StartPlacementPhase(OnBotPlacementComplete);
    }

    private void OnBotPlacementComplete()
    {
        // В первом раунде пропускаем подсчёт очков бота
        if (roundNumber == 1)
        {
            botAccumulatedScore = 0;
            UpdateUI();
            SetPhase(TurnPhase.BotAction);
            botController.StartActionPhase(OnBotActionComplete, botAccumulatedScore,
                playerGridManager, playerCharacter);
        }
        else
        {
            StartCoroutine(CalculateBotScore());
        }
    }

    private IEnumerator CalculateBotScore()
    {
        int score = 0;
        yield return StartCoroutine(botGridManager.CountingCoroutine(value => score = value));
        botAccumulatedScore += score;
        UpdateUI();
        SetPhase(TurnPhase.BotAction);
        botController.StartActionPhase(OnBotActionComplete, botAccumulatedScore,
            playerGridManager, playerCharacter);
    }

    private void OnBotActionComplete(int remainingScore)
    {
        botAccumulatedScore = remainingScore;
        UpdateUI();
        playerDeckManager.DrawTurnCards(2);
        roundNumber++;

        if (botAbilityHandler != null)
            botAbilityHandler.OnBotTurnEnd(playerGridManager);

        CheckEndGame();
        SetPhase(TurnPhase.PlayerPlacement);
    }

    private void TriggerGameOver(bool playerWon)
    {
        if (gameOverTriggered) return;
        gameOverTriggered = true;
        actionButton.interactable = false;
        OnGameOver?.Invoke(playerWon);
    }

    private void CheckEndGame()
    {
        if (gameOverTriggered) return;
        bool playerHasItems = playerGridManager.HasItemsOnField() || !playerDeckManager.IsHandEmpty || !playerDeckManager.IsDeckEmpty;
        bool botHasItems = botGridManager.HasItemsOnField() || !botDeckManager.IsHandEmpty || !botDeckManager.IsDeckEmpty;
        if (roundNumber >= 100 || (!playerHasItems && !botHasItems))
        {
            if (playerCharacter.CurrentHealth > botCharacter.CurrentHealth)
                TriggerGameOver(true);
            else
                TriggerGameOver(false);
        }
    }

    public void SpendPlayerMana(int amount) { playerMana -= amount; UpdateUI(); }
    public void SpendBotMana(int amount) { botMana -= amount; UpdateUI(); }
    public void AddPlayerMana(int amount) { playerMana += amount; UpdateUI(); }
    public void AddBotMana(int amount) { botMana += amount; UpdateUI(); }
    public bool CanAffordPlayer(int cost) => playerMana >= cost;
    public bool CanAffordBot(int cost) => botMana >= cost;
    public bool IsPlayerGridManager(GridManager manager) => manager == playerGridManager;
    public bool IsBotGridManager(GridManager manager) => manager == botGridManager;

    public void UpdateBuffDisplay()
    {
        if (buffsText == null) return;
        string text = "";
        foreach (var buff in PlayerInventory.activeBuffs)
            text += $"{buff.data.buffName} (боёв: {buff.remainingBattles})\n";
        buffsText.text = text;
    }

    private void UpdateUI()
    {
        playerScoreText.text = $"Очки игрока: {playerAccumulatedScore}";
        botScoreText.text = $"Очки бота: {botAccumulatedScore}";
        if (playerManaText != null) playerManaText.text = $"Мана: {playerMana}";
        if (botManaText != null) botManaText.text = $"Мана: {botMana}";
    }
}