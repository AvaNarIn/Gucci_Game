using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "New TicTacToe", menuName = "Items/TicTacToe Data")]
public class TicTacToeData : ItemData
{
    [Header("Параметры Крестика-Нолика")]
    public MarkTypes markType;

    public enum MarkTypes { Cross, Nought }
}