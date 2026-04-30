using UnityEngine;

[CreateAssetMenu(fileName = "New Dice", menuName = "Items/Dice Data")]
public class DiceData : ItemData
{
    [Header("Параметры кубика")]
    public TypeNumberOfFaces numberOfFaces;

    public enum TypeNumberOfFaces { D4=4, D6=6, D8=8, D10=10, D12=12, D20=20 }
}