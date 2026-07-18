using UnityEngine;

public class BlockPiece : MonoBehaviour
{
    public GameObject blockCellPrefab;
    public ShapePattern CurrentPattern { get; private set; }
    public Vector3 PivotOffset { get; private set; }

    public void SetupPiece(ShapePattern pattern, float cellSize, float gap)
    {
        CurrentPattern = pattern;
        float size = cellSize;
        float spacing = gap;

        if (pattern.cellOffsets == null) return;

        float step = size + spacing;

        // Find bounds for centering
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (Vector2Int offset in pattern.cellOffsets)
        {
            if (offset.x < minX) minX = offset.x;
            if (offset.x > maxX) maxX = offset.x;
            if (offset.y < minY) minY = offset.y;
            if (offset.y > maxY) maxY = offset.y;
        }
        int cols = (int)(maxX - minX + 1);
        int rows = (int)(maxY - minY + 1);

        float totalWidth = cols * size + (cols - 1) * spacing;
        float totalHeight = rows * size + (rows - 1) * spacing;
        PivotOffset = new Vector3(-totalWidth / 2f + size / 2f, totalHeight / 2f - size / 2f, 0f);

        foreach (Vector2Int offset in pattern.cellOffsets)
        {
            GameObject cell = Instantiate(blockCellPrefab, transform);
            float posX = offset.x * step;
            float posY = -offset.y * step;
            cell.transform.localPosition = new Vector3(posX, posY, 0f) + PivotOffset;
        }

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Destroy(box);
        }
    }
}
