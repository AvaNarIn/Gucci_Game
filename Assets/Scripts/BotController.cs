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

    private void Start()
    {
        foreach (Transform child in handPanel)
        {
            Draggable d = child.GetComponent<Draggable>();
            if (d != null)
                handItems.Add(d);
        }
    }

    public void StartPlacementPhase(System.Action onComplete)
    {
        onPlacementComplete = onComplete;
        StartCoroutine(PlacementCoroutine());
    }

    private IEnumerator PlacementCoroutine()
    {
        while (handItems.Count > 0)
        {
            Draggable chosen = handItems[Random.Range(0, handItems.Count)];
            List<int> emptyCells = botGridManager.GetEmptyCells();
            if (emptyCells.Count == 0) break;

            int targetCell = emptyCells[Random.Range(0, emptyCells.Count)];
            if (botGridManager.PlaceExistingDraggable(chosen, targetCell))
            {
                handItems.Remove(chosen);
                Debug.Log($"Бот выставил {chosen.ItemData.displayName} в ячейку {targetCell}");
            }
            yield return new WaitForSeconds(placementDelay);
        }
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
                (playerCharacter.IsAlive && Random.value < 0.3f); // 30% шанс атаковать персонажа

            if (attackCharacter)
            {
                int damage = Mathf.Min(remainingScore, playerCharacter.CurrentHealth);
                playerCharacter.TakeDamage(damage);
                remainingScore -= damage;
                Debug.Log($"Бот атакует персонажа на {damage} урона");
            }
            else
            {
                GridCell targetCell = enemyCells[Random.Range(0, enemyCells.Count)];
                int damage = Mathf.Min(remainingScore, targetCell.CurrentHealth);
                targetCell.TakeDamage(damage);
                remainingScore -= damage;
                Debug.Log($"Бот атакует клетку {targetCell.CellIndex} на {damage} урона");
                if (targetCell.currentItem == null)
                    enemyCells.Remove(targetCell);
            }
            yield return new WaitForSeconds(actionDelay);
        }
        onActionComplete?.Invoke(remainingScore);
    }
}