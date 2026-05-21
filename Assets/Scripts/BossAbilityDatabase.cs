using UnityEngine;

[CreateAssetMenu(fileName = "BossAbilityDatabase", menuName = "Game/Boss Ability Database")]
public class BossAbilityDatabase : ScriptableObject
{
    public BossAbilityData[] allAbilities;

    public BossAbilityData GetRandomAbility()
    {
        if (allAbilities == null || allAbilities.Length == 0) return null;
        return allAbilities[Random.Range(0, allAbilities.Length)];
    }
}