using UnityEngine;
using UnityEngine.UIElements;

public class SettingsPanelController : MonoBehaviour
{
    private UIDocument uiDoc;

    private const string SoundKey = "SoundEnabled";
    private const string MusicKey = "MusicEnabled";
    private const string VibrationKey = "VibrationEnabled";

    private void Awake()
    {
        uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null)
            uiDoc = gameObject.AddComponent<UIDocument>();
        uiDoc.sortingOrder = 25;
    }

    private void OnEnable()
    {
        VisualElement root = uiDoc.rootVisualElement;
        if (root == null) return;

        Toggle soundToggle = root.Q<Toggle>("soundToggle");
        Toggle musicToggle = root.Q<Toggle>("musicToggle");
        Toggle vibrationToggle = root.Q<Toggle>("vibrationToggle");
        Button backButton = root.Q<Button>("backButton");

        if (soundToggle != null)
        {
            soundToggle.value = PlayerPrefs.GetInt(SoundKey, 1) == 1;
            soundToggle.RegisterValueChangedCallback(evt =>
            {
                PlayerPrefs.SetInt(SoundKey, evt.newValue ? 1 : 0);
                PlayerPrefs.Save();
            });
        }

        if (musicToggle != null)
        {
            musicToggle.value = PlayerPrefs.GetInt(MusicKey, 1) == 1;
            musicToggle.RegisterValueChangedCallback(evt =>
            {
                PlayerPrefs.SetInt(MusicKey, evt.newValue ? 1 : 0);
                PlayerPrefs.Save();
            });
        }

        if (vibrationToggle != null)
        {
            vibrationToggle.value = PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            vibrationToggle.RegisterValueChangedCallback(evt =>
            {
                PlayerPrefs.SetInt(VibrationKey, evt.newValue ? 1 : 0);
                PlayerPrefs.Save();
            });
        }

        if (backButton != null) backButton.clicked += OnBackClicked;

        Hide();
    }

    private void OnDisable()
    {
        if (uiDoc == null) return;
        VisualElement root = uiDoc.rootVisualElement;
        if (root == null) return;
        Button backButton = root.Q<Button>("backButton");
        if (backButton != null) backButton.clicked -= OnBackClicked;
    }

    public void Show()
    {
        uiDoc.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        uiDoc.rootVisualElement.style.display = DisplayStyle.None;
    }

    private void OnBackClicked()
    {
        Hide();

        UIManager uiManager = FindAnyObjectByType<UIManager>();
        if (uiManager != null)
            uiManager.ShowMainMenu();
    }
}
