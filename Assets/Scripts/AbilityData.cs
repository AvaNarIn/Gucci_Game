using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Game/Ability Data")]
public class AbilityData : ScriptableObject
{
    public string abilityName;
    public string description;
    public ItemSet set;
    public Sprite icon;
}