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
    [SerializeField] private GridManager playerGridManager;
    [SerializeField] private GridManager botGridManager;
    [SerializeField] private BotController botController;
    [SerializeField] private Button actionButton;
    [SerializeField] private Text actionButtonText;
    [SerializeField] private Text playerScoreText;
    [SerializeField] private Text botScoreText;
    [SerializeField] private Transform playerHandPanel;
    [SerializeField] private Transform botHandPanel;
    [SerializeField] private Character playerCharacter;
    [SerializeField] private Character botCharacter;

    private TurnPhase currentPhase;
    private int playerAccumulatedScore = 0;
    private int botAccumulatedScore = 0;

    private List<Draggable> subscribedDraggables = new List<Draggable>();

    private void Start()
    {
        InitializeHand(playerGridManager, playerHandPanel, true);
        InitializeHand(botGridManager, botHandPanel, false);
        SetPhase(TurnPhase.PlayerPlacement);
        UpdateUI();
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
                actionButtonText.text = "Подсчитать очки";
                actionButton.interactable = true;
                break;
            case TurnPhase.PlayerAction:
                actionButtonText.text = "Завершить ход";
                actionButton.interactable = true;
                SubscribeToEnemyDraggables();
                break;
            case TurnPhase.BotPlacement:
            case TurnPhase.BotAction:
                actionButton.interactable = false;
                break;
        }
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
            CheckGameOver();
        }
    }

    private void CheckGameOver()
    {
        if (!playerCharacter.IsAlive)
            Debug.Log("Бот победил");
        else if (!botCharacter.IsAlive)
            Debug.Log("Игрок победил");
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
                StartCoroutine(CalculatePlayerScore());
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
        SetPhase(TurnPhase.BotPlacement);
        botController.StartPlacementPhase(OnBotPlacementComplete);
    }

    private void OnBotPlacementComplete()
    {
        StartCoroutine(CalculateBotScore());
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
        CheckGameOver();
        SetPhase(TurnPhase.PlayerPlacement);
    }

    private void UpdateUI()
    {
        playerScoreText.text = $"Очки игрока: {playerAccumulatedScore}";
        botScoreText.text = $"Очки бота: {botAccumulatedScore}";
    }
}