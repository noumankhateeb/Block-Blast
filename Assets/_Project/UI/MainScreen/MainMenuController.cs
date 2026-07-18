using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private UIDocument uiDoc;
    private Label highScoreValue;

    private const string HighScoreKey = "HighScore";

    private void Awake()
    {
        uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null)
            uiDoc = gameObject.AddComponent<UIDocument>();
        uiDoc.sortingOrder = 10;
    }

    private void OnEnable()
    {
        VisualElement root = uiDoc.rootVisualElement;
        if (root == null) return;

        highScoreValue = root.Q<Label>("highScoreValue");

        Button playButton = root.Q<Button>("playButton");
        Button settingsButton = root.Q<Button>("settingsButton");

        if (playButton != null) playButton.clicked += OnPlayClicked;
        if (settingsButton != null) settingsButton.clicked += OnSettingsClicked;

        LoadHighScore();
    }

    private void OnDisable()
    {
        if (uiDoc == null) return;
        VisualElement root = uiDoc.rootVisualElement;
        if (root == null) return;
        Button playButton = root.Q<Button>("playButton");
        Button settingsButton = root.Q<Button>("settingsButton");

        if (playButton != null) playButton.clicked -= OnPlayClicked;
        if (settingsButton != null) settingsButton.clicked -= OnSettingsClicked;
    }

    private void LoadHighScore()
    {
        int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (highScoreValue != null)
            highScoreValue.text = highScore.ToString();
    }

    private void OnPlayClicked()
    {
        UIManager uiManager = FindAnyObjectByType<UIManager>();
        if (uiManager != null)
            uiManager.StartGame();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnSettingsClicked()
    {
        UIManager uiManager = FindAnyObjectByType<UIManager>();
        if (uiManager != null)
            uiManager.ShowSettings();
    }
}
