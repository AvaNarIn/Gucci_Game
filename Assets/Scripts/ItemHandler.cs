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
    public int animationDuration;
    public Dictionary<AbilityData, bool> ActiveAbilities { get; private set; } = new Dictionary<AbilityData, bool>();

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

    public abstract IEnumerator ApplyingEffects_Coroutine();
    public abstract IEnumerator CountingScore_Coroutine();
}