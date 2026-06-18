using System.Collections.Generic;

public static class PlayerInventory
{
    public static List<ItemData> cards = new List<ItemData>();
    public static List<AbilityData> abilities = new List<AbilityData>();
    public static List<BuffInstance> activeBuffs = new List<BuffInstance>();
    public static int maxAbilities = 1;   // будет меняться LevelManager

    public static bool AddCard(ItemData card)
    {
        if (cards.Count >= 45) return false;
        cards.Add(card);
        return true;
    }

    public static bool RemoveCard(ItemData card)
    {
        return cards.Remove(card);
    }

    public static bool ReplaceCard(ItemData oldCard, ItemData newCard)
    {
        int idx = cards.IndexOf(oldCard);
        if (idx < 0) return false;
        cards[idx] = newCard;
        return true;
    }

    public static bool AddAbility(AbilityData ability)
    {
        if (abilities.Count >= maxAbilities) return false;
        abilities.Add(ability);
        return true;
    }

    public static void ReplaceAbility(AbilityData oldAbility, AbilityData newAbility)
    {
        int idx = abilities.IndexOf(oldAbility);
        if (idx >= 0) abilities[idx] = newAbility;
    }

    public static void RemoveExpiredBuffs()
    {
        activeBuffs.RemoveAll(b => b.remainingBattles <= 0);
    }

    public static void DecreaseBuffDurations()
    {
        foreach (var buff in activeBuffs)
            buff.remainingBattles--;
        RemoveExpiredBuffs();
    }
}