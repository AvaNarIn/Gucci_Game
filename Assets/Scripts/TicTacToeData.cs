using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "New TicTacToe", menuName = "Game/TicTacToe Data")]
public class TicTacToeData : ItemData
{
    public enum MarkType { Cross, Nought }
    public MarkType type;
}