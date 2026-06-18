public static class LevelManager
{
    public static CharacterData selectedCharacter;
    public static int currentLevel = 1;            // 1..6
    public static int bossesDefeatedThisRun = 0;  // сколько боссов побеждено в текущем забеге
    public static int bossesRequired => currentLevel; // для прохождения уровня нужно победить столько боссов
    public static bool levelCompleted = false;     // становится true при выполнении условия
    public static bool runActive = false;          // идёт ли забег

    public static void StartRun(CharacterData character, int level)
    {
        selectedCharacter = character;
        currentLevel = level;
        bossesDefeatedThisRun = 0;
        levelCompleted = false;
        runActive = true;

        // Очищаем инвентарь и загружаем стартовую колоду
        PlayerInventory.cards.Clear();
        foreach (var card in character.startingDeck)
            PlayerInventory.cards.Add(card);

        // Сбрасываем способности и баффы
        PlayerInventory.abilities.Clear();
        PlayerInventory.activeBuffs.Clear();

        // Устанавливаем лимит способностей равным текущему уровню
        PlayerInventory.maxAbilities = currentLevel;
    }

    public static void OnBossDefeated()
    {
        if (!runActive || levelCompleted) return;
        bossesDefeatedThisRun++;
        if (bossesDefeatedThisRun >= bossesRequired)
        {
            levelCompleted = true;
        }
    }

    public static void EndRun()
    {
        runActive = false;
    }
}