using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour
{
    private static GameOverScreen instance;
    private GameObject panel;
    private RectTransform restartButtonRect;

    void Awake()
    {
        instance = this;
        CreateUI();
        panel.SetActive(false);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void Update()
    {
        if (panel == null || !panel.activeInHierarchy) return;

        if (Input.GetMouseButtonUp(0) && restartButtonRect != null)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                restartButtonRect, Input.mousePosition, null, out localPoint);
            if (restartButtonRect.rect.Contains(localPoint))
                RestartGame();
        }
    }

    void CreateUI()
    {
        GameObject canvasGO = new GameObject("GameOverCanvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvasGO.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.65f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGO.transform, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.12f, 0.12f, 0.15f, 1);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(320, 240);

        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(panel.transform, false);
        Text title = titleGO.AddComponent<Text>();
        title.text = "GAME OVER";
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 44;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(280, 60);
        titleRect.anchoredPosition = new Vector2(0, 50);

        GameObject btnGO = new GameObject("RestartButton");
        btnGO.transform.SetParent(panel.transform, false);
        Image btnImage = btnGO.AddComponent<Image>();
        btnImage.color = new Color(0.25f, 0.55f, 0.25f, 1);
        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(RestartGame);
        restartButtonRect = btnGO.GetComponent<RectTransform>();
        restartButtonRect.sizeDelta = new Vector2(200, 55);
        restartButtonRect.anchoredPosition = new Vector2(0, -30);

        GameObject btnTextGO = new GameObject("ButtonText");
        btnTextGO.transform.SetParent(btnGO.transform, false);
        Text btnText = btnTextGO.AddComponent<Text>();
        btnText.text = "RESTART";
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 28;
        btnText.fontStyle = FontStyle.Bold;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        RectTransform btnTextRect = btnTextGO.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
    }

    public static void Show()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("GameOverScreen");
            instance = go.AddComponent<GameOverScreen>();
        }
        instance.panel.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
