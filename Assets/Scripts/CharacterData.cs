using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public string description;
    public ItemSet set;
    public List<ItemData> startingDeck;

    [Header("”словие разблокировки")]
    public CharacterData requiredCharacter;   // если null Ц доступен сразу
    public int requiredLevel;
}