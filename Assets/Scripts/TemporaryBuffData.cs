using UnityEngine;

[CreateAssetMenu(fileName = "NewBuff", menuName = "Game/Temporary Buff Data")]
public class TemporaryBuffData : ScriptableObject
{
    public string buffName;
    public string description;
    public Sprite icon;
}