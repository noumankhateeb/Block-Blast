using UnityEngine;

[CreateAssetMenu(menuName = "Block Blast/Theme")]
public class ThemeData : ScriptableObject
{
    public string themeName;
    
    [Header("Board Sprites")]
    public Sprite boardSprite;         // Your Board background
    
    [Header("Placeholder Sprites")]
    public Sprite emptyCellSprite;     // Your Empty Square
    
    [Header("Block Sprites (By Piece Type)")]
    public Sprite[] blockSprites;      // 0: Square, 1: L-Block, etc.

    [Header("UI & Background")]
    public Color cameraBackgroundColor = Color.black;
}