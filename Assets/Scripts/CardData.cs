using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Game/Card Data")]
public class CardData : ItemData
{
    [Header("Параметры карты")]
    public string value;
    public Suit suit;

    public enum Suit { Hearts, Diamonds, Clubs, Spades, Joker }
}