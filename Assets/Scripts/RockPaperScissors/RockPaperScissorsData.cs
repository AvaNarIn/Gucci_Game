using UnityEngine;

[CreateAssetMenu(fileName = "New RockPaperScissor", menuName = "Items/RockPaperScissor Data")]

public class RockPaperScissorsData : ItemData
{
    [Header("Параметры Камня-Ножницы-Бумаги")]
    public Shapes shape;

    public enum Shapes { Rock, Paper, Scissors }
}
