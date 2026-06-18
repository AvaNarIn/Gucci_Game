using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public abstract class ItemHandler : MonoBehaviour
{
    public float LastScore { get; protected set; }
    protected GridManager gridManager;

    [Header("Набор предметов, который обрабатывает этот Handler")]
    public ItemSet set;

    public AbilityDatabase abilityDatabase;
    public float animationDuration;

    public Dictionary<AbilityData, bool> ActiveAbilities { get; private set; } = new Dictionary<AbilityData, bool>();

    private static Dictionary<string, object> abilityStateStore = new Dictionary<string, object>();

    protected virtual void Awake()
    {
        gridManager = GetComponent<GridManager>();
    }

    public void InitAbilities()
    {
        ActiveAbilities.Clear();
        if (abilityDatabase != null)
        {
            foreach (var ability in abilityDatabase.allAbilities)
            {
                if (ability.set == set)
                    ActiveAbilities[ability] = false;
            }
        }
    }

    public void RefreshAbilities()
    {
        List<AbilityData> keys = new List<AbilityData>(ActiveAbilities.Keys);
        foreach (var key in keys)
            ActiveAbilities[key] = false;

        foreach (var ability in PlayerInventory.abilities)
        {
            if (ability.set == set && ActiveAbilities.ContainsKey(ability))
                ActiveAbilities[ability] = true;
        }
    }

    public bool IsAbilityActive(AbilityData ability)
    {
        return ActiveAbilities.ContainsKey(ability) && ActiveAbilities[ability];
    }

    public AbilityData GetAbilityByName(string name)
    {
        foreach (var ability in ActiveAbilities.Keys)
        {
            if (ability.abilityName == name) return ability;
        }
        return null;
    }

    public bool HasAbility(string name)
    {
        foreach (var ability in ActiveAbilities.Keys)
        {
            if (ability.abilityName == name && ActiveAbilities[ability])
                return true;
        }
        return false;
    }

    public static void SetAbilityState(string abilityName, object state)
    {
        abilityStateStore[abilityName] = state;
    }

    public static object GetAbilityState(string abilityName)
    {
        abilityStateStore.TryGetValue(abilityName, out object value);
        return value;
    }

    public static void RemoveAbilityState(string abilityName)
    {
        abilityStateStore.Remove(abilityName);
    }

    public static string GetAbilityCustomDescription(AbilityData ability)
    {
        if (ability.abilityName == "Усиление комбинации")
        {
            string combo = GetAbilityState(ability.abilityName) as string;
            if (!string.IsNullOrEmpty(combo))
                return ability.description + "\nВыбранная комбинация: " + combo;
        }
        return ability.description;
    }

    public abstract IEnumerator ApplyingEffects_Coroutine();
    public abstract IEnumerator CountingScore_Coroutine();
}