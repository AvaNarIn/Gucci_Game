using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Персонажи")]
    public CharacterData[] allCharacters;
    public Transform characterButtonContainer;
    public GameObject characterButtonPrefab;

    [Header("Уровни")]
    public GameObject levelSelectionPanel;
    public Transform levelButtonContainer;
    public GameObject levelButtonPrefab;

    [Header("Прогресс")]
    public Button resetProgressButton;

    private CharacterData selectedCharacter;

    public GameObject TrainingImage;

    void Start()
    {
        resetProgressButton.onClick.AddListener(ResetProgress);
        PopulateCharacters();
    }

    public void TrainingClick()
    {
        TrainingImage.SetActive(!TrainingImage.activeSelf);
    }

    void PopulateCharacters()
    {
        // Очищаем старые кнопки, не забывая отписаться от событий
        foreach (Transform child in characterButtonContainer)
        {
            if (child == null) continue;
            Button btn = child.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners();
            Destroy(child.gameObject);
        }

        foreach (var character in allCharacters)
        {
            GameObject btnGO = Instantiate(characterButtonPrefab, characterButtonContainer);
            Text text = btnGO.GetComponentInChildren<Text>();
            Button btn = btnGO.GetComponent<Button>();

            if (btn == null || text == null)
            {
                Debug.LogError("CharacterButtonPrefab must have a Button and a Text component.");
                continue;
            }

            bool unlocked = ProgressManager.IsCharacterUnlocked(character);
            int maxLevel = ProgressManager.GetMaxLevel(character);

            if (unlocked)
            {
                text.text = $"{character.characterName}\n(Ур. {maxLevel})\n{character.description}";
                btn.interactable = true;
                btn.onClick.AddListener(() => OnCharacterClicked(character));
            }
            else
            {
                string requirement = "";
                if (character.requiredCharacter != null)
                    requirement = $"Требуется: {character.requiredCharacter.characterName} ур.{character.requiredLevel}";
                text.text = $"{character.characterName}\n{requirement}\n{character.description}";
                btn.interactable = false;
            }
        }
    }

    void OnCharacterClicked(CharacterData character)
    {
        selectedCharacter = character;
        ShowLevelSelection(character);
    }

    void ShowLevelSelection(CharacterData character)
    {
        levelSelectionPanel.SetActive(true);

        // Очищаем старые кнопки уровней с отпиской
        foreach (Transform child in levelButtonContainer)
        {
            if (child == null) continue;
            Button btn = child.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners();
            Destroy(child.gameObject);
        }

        int maxLevel = ProgressManager.GetMaxLevel(character);

        for (int level = 1; level <= 6; level++)
        {
            int currentLevel = level;
            GameObject btnGO = Instantiate(levelButtonPrefab, levelButtonContainer);
            Text text = btnGO.GetComponentInChildren<Text>();
            Button btn = btnGO.GetComponent<Button>();

            if (btn == null || text == null)
            {
                Debug.LogError("LevelButtonPrefab must have a Button and a Text component.");
                continue;
            }

            text.text = $"Уровень {currentLevel}";

            if (level <= maxLevel)
            {
                text.text += " (пройден)";
                btn.interactable = true;
            }
            else if (level == maxLevel + 1)
            {
                btn.interactable = true;
            }
            else
            {
                btn.interactable = false;
            }

            btn.onClick.AddListener(() => OnLevelClicked(currentLevel));
        }
    }

    void OnLevelClicked(int level)
    {
        if (selectedCharacter == null) return;
        LevelManager.StartRun(selectedCharacter, level);
        SceneManager.LoadScene("BattleScene");
    }

    void ResetProgress()
    {
        ProgressManager.ResetProgress();
        PopulateCharacters();
        levelSelectionPanel.SetActive(false);
    }
}