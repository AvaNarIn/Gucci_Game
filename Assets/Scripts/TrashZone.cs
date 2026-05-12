using UnityEngine;
using UnityEngine.EventSystems;

public class TrashZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        Draggable d = eventData.pointerDrag?.GetComponent<Draggable>();
        if (d != null)
        {
            d.DestroyItem();

            if (d.OwnerGridManager == TurnManager.Instance.PlayerGridManager)
                TurnManager.Instance.AddPlayerMana(1);
            else if (d.OwnerGridManager == TurnManager.Instance.BotGridManager)
                TurnManager.Instance.AddBotMana(1);
        }
    }
}