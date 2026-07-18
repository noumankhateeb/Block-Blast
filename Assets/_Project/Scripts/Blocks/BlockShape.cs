using UnityEngine;

[System.Serializable]
public struct ShapePattern
{
    public string shapeName;
    public Vector2Int[] cellOffsets; 
    public Color preferredColor;
}

[CreateAssetMenu(menuName = "Block Blast/Block Shape")]
public class BlockShape : ScriptableObject
{
    public ShapePattern[] shapes;
}