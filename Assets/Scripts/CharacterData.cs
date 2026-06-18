using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public string description;
    public Sprite icon;
    public ItemSet set;
    public List<ItemData> startingDeck;

    [Header("Условие разблокировки")]
    public CharacterData requiredCharacter;   // если null – доступен сразу
    public int requiredLevel;
}