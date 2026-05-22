using UnityEngine;

public static class ProgressManager
{
    public static int GetMaxLevel(CharacterData character)
    {
        if (character == null) return 0;
        return PlayerPrefs.GetInt("Char_" + character.characterName, 0);
    }

    public static void SetMaxLevel(CharacterData character, int level)
    {
        if (character == null) return;
        int current = GetMaxLevel(character);
        if (level > current)
            PlayerPrefs.SetInt("Char_" + character.characterName, level);
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteAll();
    }

    public static bool IsCharacterUnlocked(CharacterData character)
    {
        if (character == null) return false;
        if (character.requiredCharacter == null) return true;
        return GetMaxLevel(character.requiredCharacter) >= character.requiredLevel;
    }
}