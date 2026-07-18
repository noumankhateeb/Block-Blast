using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int CurrentScore { get; private set; }
    public int HighScore { get; private set; }

    private const string HighScoreKey = "HighScore";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    public void AddScore(int points)
    {
        CurrentScore += points;
        UIManager.Instance?.UpdateScore(CurrentScore);
    }

    public void AddLineClearScore(int linesCleared, int cellsCleared)
    {
        int basePoints = cellsCleared * 10;
        int lineBonus = linesCleared * 50;
        int comboMultiplier = linesCleared > 1 ? linesCleared : 1;
        int totalPoints = (basePoints + lineBonus) * comboMultiplier;

        AddScore(totalPoints);

        if (linesCleared > 1)
        {
            UIManager.Instance?.ShowCombo($"{linesCleared}x COMBO +{totalPoints}");
        }
    }

    public void OnGameOver()
    {
        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
            UIManager.Instance?.UpdateHighScore(HighScore);
        }

        UIManager.Instance?.ShowGameOver(CurrentScore);
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        UIManager.Instance?.UpdateScore(0);
    }
}