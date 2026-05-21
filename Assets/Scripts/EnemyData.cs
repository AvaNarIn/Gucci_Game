using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string abilityDescription;
    public ItemSet set1;
    public ItemSet set2;
    public RewardType[] rewards;

    public enum RewardType { Item, Cell, Ability, Random }
}