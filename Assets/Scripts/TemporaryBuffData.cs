using UnityEngine;

[CreateAssetMenu(fileName = "NewBuff", menuName = "Buffs/Temporary Buff Data")]
public class TemporaryBuffData : ScriptableObject
{
    public string buffName;
    public string description;
    public Sprite icon;
}