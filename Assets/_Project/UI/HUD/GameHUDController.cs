using UnityEngine;
using UnityEngine.UIElements;

public class GameHUDController : MonoBehaviour
{
    private UIDocument uiDoc;

    private Label scoreValue;
    private Label bestValue;
    private Label levelLabel;
    private Label comboText;
    private int displayedScore = 0;
    private int targetScore = 0;

    private const string HighScoreKey = "HighScore";

    private void Awake()
    {
        uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null)
            uiDoc = gameObject.AddComponent<UIDocument>();
        uiDoc.sortingOrder = 5;
    }

    private void OnEnable()
    {
        VisualElement root = uiDoc.rootVisualElement;
        if (root == null) return;

        scoreValue = root.Q<Label>("scoreValue");
        bestValue = root.Q<Label>("bestValue");
        levelLabel = root.Q<Label>("levelLabel");
        comboText = root.Q<Label>("comboText");

        int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (bestValue != null)
            bestValue.text = highScore.ToString();
    }

    private void Update()
    {
        if (displayedScore != targetScore)
        {
            displayedScore = (int)Mathf.MoveTowards(displayedScore, targetScore, Time.deltaTime * 20f);
            scoreValue.text = displayedScore.ToString();
        }
    }

    public void SetScore(int score)
    {
        targetScore = score;
    }

    public void UpdateHighScore(int highScore)
    {
        bestValue.text = highScore.ToString();
    }

    public void ShowCombo(string text)
    {
        if (comboText != null)
        {
            comboText.text = text;
            comboText.style.opacity = 1f;
            CancelInvoke(nameof(HideCombo));
            Invoke(nameof(HideCombo), 1.5f);
        }
    }

    private void HideCombo()
    {
        if (comboText != null)
            comboText.style.opacity = 0f;
    }

    public void SetLevel(int level)
    {
        if (levelLabel != null)
            levelLabel.text = $"LEVEL {level}";
    }
}
