using UnityEngine;

[CreateAssetMenu(fileName = "NewBossAbility", menuName = "Game/Boss Ability Data")]
public class BossAbilityData : ScriptableObject
{
    public string abilityName;
    public string description;
    public Sprite icon;
}