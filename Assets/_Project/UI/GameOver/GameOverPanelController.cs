using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class GameOverPanelController : MonoBehaviour
{
    private UIDocument uiDoc;

    private Label finalScoreValue;
    private Label bestValue;
    private VisualElement newBestBadge;

    private const string HighScoreKey = "HighScore";

    private void Awake()
    {
        uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null)
            uiDoc = gameObject.AddComponent<UIDocument>();
        uiDoc.sortingOrder = 20;
    }

    private void OnEnable()
    {
        VisualElement root = uiDoc.rootVisualElement;
        if (root == null) return;

        finalScoreValue = root.Q<Label>("finalScoreValue");
        bestValue = root.Q<Label>("bestValue");
        newBestBadge = root.Q<VisualElement>("newBestBadge");

        Button restartButton = root.Q<Button>("restartButton");
        Button menuButton = root.Q<Button>("menuButton");

        if (restartButton != null) restartButton.clicked += OnRestartClicked;
        if (menuButton != null) menuButton.clicked += OnMenuClicked;

        Hide();
    }

    private void OnDisable()
    {
        if (uiDoc == null) return;
        VisualElement root = uiDoc.rootVisualElement;
        if (root == null) return;
        Button restartButton = root.Q<Button>("restartButton");
        Button menuButton = root.Q<Button>("menuButton");

        if (restartButton != null) restartButton.clicked -= OnRestartClicked;
        if (menuButton != null) menuButton.clicked -= OnMenuClicked;
    }

    public void Show(int finalScore)
    {
        VisualElement root = uiDoc.rootVisualElement;
        root.style.display = DisplayStyle.Flex;

        int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        bool isNewBest = finalScore > highScore;

        if (isNewBest)
        {
            highScore = finalScore;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        finalScoreValue.text = finalScore.ToString();
        bestValue.text = highScore.ToString();
        newBestBadge.style.opacity = isNewBest ? 1f : 0f;
    }

    public void Hide()
    {
        VisualElement root = uiDoc.rootVisualElement;
        root.style.display = DisplayStyle.None;
    }

    private void OnRestartClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnMenuClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
