using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance;

    public ThemeData[] availableThemes;
    public ThemeData activeTheme;

    [Header("Scene References")]
    public SpriteRenderer boardRenderer;       
    public GridSystem gridSystem;              

    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            // Apply theme immediately so data is ready before other scripts hit Start()
            ApplyTheme(activeTheme);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ApplyTheme(ThemeData newTheme)
    {
        if (newTheme == null) return;

        activeTheme = newTheme;
        
        // 1. Change Camera Background Color
        if (Camera.main != null)
            Camera.main.backgroundColor = activeTheme.cameraBackgroundColor;
        
        // 2. Swap Board Sprite
        if (boardRenderer != null) 
            boardRenderer.sprite = activeTheme.boardSprite;

        // 3. Tell the Grid to swap all 64 Placeholder Sprites
        if (gridSystem != null)
            gridSystem.UpdateTheme(activeTheme);
    }
}