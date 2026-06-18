using UnityEngine;

[CreateAssetMenu(fileName = "New Chess", menuName = "Items/Chess Data")]

public class ChessData : ItemData
{
    [Header("Параметры фигуры")]
    public TypesOfChessPiece TypeOfChessPiece;

    public enum TypesOfChessPiece { Pawn, Knight, Bishop, Rook, Queen, King}
}
