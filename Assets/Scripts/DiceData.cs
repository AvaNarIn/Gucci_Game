using UnityEngine;

[CreateAssetMenu(fileName = "New Dice", menuName = "Game/Dice Data")]
public class DiceData : ItemData
{
    [Header("Параметры кубика")]
    [Range(4, 20)]
    public int numberOfFaces = 6;
}