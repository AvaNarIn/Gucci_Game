using UnityEngine;
public enum ItemSet { Dice, Card, Chess, RockPaperScissors, TicTacToe }
[CreateAssetMenu(fileName = "New Item Data", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Общие данные")]
    public string displayName;
    public int score;
    public Sprite icon;
}