public struct EnemyInfo
{
    public string abilityDescription;
    public ItemSet set1;
    public ItemSet set2;
    public RewardType[] rewards;
    public int health;
    public bool isBoss;

    public enum RewardType { Item, Cell, Ability, TemporaryBuff, Random }
}