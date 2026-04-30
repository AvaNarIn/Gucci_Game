using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Items/Card Data")]
public class CardData : ItemData
{
    [Header("Параметры карты")]
    public Values value;
    public Suits suit;

    public enum Values { Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }
    public enum Suits { Hearts, Diamonds, Clubs, Spades }
}