using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AbilityDatabase", menuName = "Game/Ability Database")]
public class AbilityDatabase : ScriptableObject
{
    public List<AbilityData> allAbilities;
    private Dictionary<ItemSet, List<AbilityData>> lookup;

    public void Init()
    {
        lookup = new Dictionary<ItemSet, List<AbilityData>>();
        ItemSet[] sets = (ItemSet[])System.Enum.GetValues(typeof(ItemSet));   // явное приведение
        foreach (var set in sets)
            lookup[set] = new List<AbilityData>();
        foreach (var a in allAbilities)
            lookup[a.set].Add(a);
    }

    public AbilityData GetRandomAbility(ItemSet set, List<AbilityData> exclude)
    {
        if (!lookup.ContainsKey(set)) return null;
        var pool = lookup[set].FindAll(a => !exclude.Contains(a));
        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }
}