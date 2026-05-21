[System.Serializable]
public class BuffInstance
{
    public TemporaryBuffData data;
    public int remainingBattles;

    public BuffInstance(TemporaryBuffData data, int duration)
    {
        this.data = data;
        this.remainingBattles = duration;
    }
}