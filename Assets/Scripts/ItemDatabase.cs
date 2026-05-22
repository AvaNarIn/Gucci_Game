using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> allItems;
    private Dictionary<ItemSet, List<ItemData>> lookup;

    public void Init()
    {
        lookup = new Dictionary<ItemSet, List<ItemData>>();
        ItemSet[] sets = (ItemSet[])System.Enum.GetValues(typeof(ItemSet));
        foreach (var set in sets)
            lookup[set] = new List<ItemData>();
        foreach (var item in allItems)
            lookup[item.set].Add(item);
    }

    public ItemData GetRandomItem(ItemSet set)
    {
        if (!lookup.ContainsKey(set) || lookup[set].Count == 0) return null;
        return lookup[set][Random.Range(0, lookup[set].Count)];
    }

    public ItemData GetRandomItemExcluding(ItemSet exclude1, ItemSet exclude2)
    {
        var possible = new List<ItemData>();
        foreach (var kv in lookup)
            if (kv.Key != exclude1 && kv.Key != exclude2)
                possible.AddRange(kv.Value);
        if (possible.Count == 0) return null;
        return possible[Random.Range(0, possible.Count)];
    }
}