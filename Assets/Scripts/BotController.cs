using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotController : MonoBehaviour
{
    [SerializeField] private GridManager botGridManager;
    [SerializeField] private Transform handPanel;
    [SerializeField] private float placementDelay = 1f;
    [SerializeField] private float actionDelay = 1f;

    private List<Draggable> handItems = new List<Draggable>();
    private System.Action onPlacementComplete;
    private System.Action<int> onActionComplete;

    public void StartPlacementPhase(System.Action onComplete)
    {
        onPlacementComplete = onComplete;
        handItems.Clear();
        foreach (Transform child in handPanel)
        {
            Draggable d = child.GetComponent<Draggable>();
            if (d != null) handItems.Add(d);
        }

        if (handItems.Count >= 6)
        {
            Draggable cheapest = handItems[0];
            for (int i = 1; i < handItems.Count; i++)
            {
                if (handItems[i].ItemData.score < cheapest.ItemData.score)
                    cheapest = handItems[i];
            }
            cheapest.DestroyItem();
            TurnManager.Instance.AddBotMana(1);
            handItems.Remove(cheapest);
        }

        StartCoroutine(PlacementCoroutine());
    }

    private IEnumerator PlacementCoroutine()
    {
        bool placed;
        do
        {
            placed = false;
            if (handItems.Count == 0) break;

            List<Draggable> affordableCards = handItems.FindAll(d => TurnManager.Instance.CanAffordBot(d.ItemData.score));
            if (affordableCards.Count == 0) break;

            Draggable chosen = affordableCards[Random.Range(0, affordableCards.Count)];
            List<int> emptyCells = botGridManager.GetEmptyCells();
            if (emptyCells.Count == 0) break;

            int targetCell = emptyCells[Random.Range(0, emptyCells.Count)];
            if (botGridManager.PlaceExistingDraggable(chosen, targetCell))
            {
                handItems.Remove(chosen);
                placed = true;
            }
            yield return new WaitForSeconds(placementDelay);
        } while (placed && handItems.Count > 0 && botGridManager.GetEmptyCells().Count > 0);

        onPlacementComplete?.Invoke();
    }

    public void StartActionPhase(System.Action<int> onComplete, int botScore,
        GridManager playerGridManager, Character playerCharacter)
    {
        onActionComplete = onComplete;
        StartCoroutine(ActionCoroutine(botScore, playerGridManager, playerCharacter));
    }

    private IEnumerator ActionCoroutine(int botScore, GridManager playerGridManager, Character playerCharacter)
    {
        int remainingScore = botScore;
        List<GridCell> enemyCells = new List<GridCell>();
        foreach (var cell in playerGridManager.GetCells())
        {
            if (cell.currentItem != null)
                enemyCells.Add(cell);
        }

        while (remainingScore > 0 && (enemyCells.Count > 0 || playerCharacter.IsAlive))
        {
            bool attackCharacter = enemyCells.Count == 0 ||
                (playerCharacter.IsAlive && Random.value < 0.3f);

            if (attackCharacter)
            {
                int damage = Mathf.Min(remainingScore, playerCharacter.CurrentHealth);
                playerCharacter.TakeDamage(damage);
                remainingScore -= damage;
            }
            else
            {
                GridCell targetCell = enemyCells[Random.Range(0, enemyCells.Count)];
                int damage = Mathf.Min(remainingScore, targetCell.CurrentHealth);
                targetCell.TakeDamage(damage);
                remainingScore -= damage;
                if (targetCell.currentItem == null)
                    enemyCells.Remove(targetCell);
            }
            yield return new WaitForSeconds(actionDelay);
        }
        onActionComplete?.Invoke(remainingScore);
    }
}