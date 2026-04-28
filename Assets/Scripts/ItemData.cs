using UnityEngine;

[CreateAssetMenu(fileName = "New Item Data", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Общие данные")]
    public string displayName;
    public int score;
    public Sprite icon;
}