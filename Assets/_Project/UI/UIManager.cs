using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Screen References")]
    private MainMenuController mainMenu;
    private GameHUDController gameHUD;
    private GameOverPanelController gameOverPanel;
    private SettingsPanelController settingsPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        mainMenu = GetComponentInChildren<MainMenuController>(true);
        gameHUD = GetComponentInChildren<GameHUDController>(true);
        gameOverPanel = GetComponentInChildren<GameOverPanelController>(true);
        settingsPanel = GetComponentInChildren<SettingsPanelController>(true);

        HideAll();
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        HideAll();
        if (mainMenu != null) mainMenu.gameObject.SetActive(true);
    }

    public void ShowHUD()
    {
        if (gameHUD != null) gameHUD.gameObject.SetActive(true);
    }

    public void HideHUD()
    {
        if (gameHUD != null) gameHUD.gameObject.SetActive(false);
    }

    public void ShowGameOver(int finalScore)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.gameObject.SetActive(true);
            gameOverPanel.Show(finalScore);
        }
    }

    public void ShowSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.gameObject.SetActive(true);
        }
    }

    public void HideAll()
    {
        if (mainMenu != null) mainMenu.gameObject.SetActive(false);
        if (gameHUD != null) gameHUD.gameObject.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.gameObject.SetActive(false);
        if (settingsPanel != null) settingsPanel.gameObject.SetActive(false);
    }

    public void UpdateScore(int score)
    {
        if (gameHUD != null) gameHUD.SetScore(score);
    }

    public void UpdateHighScore(int highScore)
    {
        if (gameHUD != null) gameHUD.UpdateHighScore(highScore);
    }

    public void ShowCombo(string text)
    {
        if (gameHUD != null) gameHUD.ShowCombo(text);
    }
}
