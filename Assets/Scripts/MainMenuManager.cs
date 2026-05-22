using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Персонажи")]
    public CharacterData[] allCharacters;   // все 10 персонажей (пока два)
    public Transform characterButtonContainer;
    public GameObject characterButtonPrefab; // префаб кнопки персонажа (Button + Text)

    [Header("Уровни")]
    public GameObject levelSelectionPanel;
    public Transform levelButtonContainer;
    public GameObject levelButtonPrefab;    // префаб кнопки уровня (Button + Text)

    private CharacterData selectedCharacter;
    private int selectedLevel;

    void Start()
    {
        PopulateCharacters();
    }

    void PopulateCharacters()
    {
        foreach (var character in allCharacters)
        {
            GameObject btnGO = Instantiate(characterButtonPrefab, characterButtonContainer);
            btnGO.GetComponentInChildren<Text>().text = $"{character.characterName}\n{character.description}";
            btnGO.GetComponent<Button>().onClick.AddListener(() => OnCharacterClicked(character));
        }
    }

    void OnCharacterClicked(CharacterData character)
    {
        selectedCharacter = character;
        ShowLevelSelection();
    }

    void ShowLevelSelection()
    {
        levelSelectionPanel.SetActive(true);
        foreach (Transform child in levelButtonContainer)
            Destroy(child.gameObject);

        for (int level = 1; level <= 6; level++)
        {
            int currentLevel = level; // capture
            GameObject btnGO = Instantiate(levelButtonPrefab, levelButtonContainer);
            btnGO.GetComponentInChildren<Text>().text = $"Уровень {currentLevel}";
            Button btn = btnGO.GetComponent<Button>();
            // Здесь можно сохранять прогресс (например, в PlayerPrefs) и отключать недоступные уровни.
            // Пока все уровни доступны.
            btn.interactable = true;
            btn.onClick.AddListener(() => OnLevelClicked(currentLevel));
        }
    }

    void OnLevelClicked(int level)
    {
        selectedLevel = level;
        LevelManager.StartRun(selectedCharacter, selectedLevel);
        SceneManager.LoadScene("BattleScene");  // имя вашей боевой сцены
    }
}