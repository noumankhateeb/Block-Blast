using UnityEngine;

public class GridSystem : MonoBehaviour
{
    [Header("Grid Dimensions")]
    public int rows = 8;
    public int columns = 8;

    [Header("Visual Settings")]
    public GameObject cellPrefab;
    public float gapBetweenCells = 0.02f;
    public Color cellColor = new Color(0.5f, 0.5f, 0.55f, 1f);

    private ThemeManager themeManager;
    public BlockSpawner blockSpawner;

    public float CellSize { get; private set; }
    public float Step { get; private set; }
    public float GridStartX { get; private set; }
    public float GridStartY { get; private set; }

    private void Start()
    {
        themeManager = ThemeManager.Instance;
        GenerateGrid();

        if (blockSpawner != null)
        {
            blockSpawner.SpawnNewSet();
        }
    }

    public void GenerateGrid()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        GameObject tempCell = Instantiate(cellPrefab);
        SpriteRenderer prefabRenderer = tempCell.GetComponent<SpriteRenderer>();
        CellSize = prefabRenderer.bounds.size.x;
        Destroy(tempCell);

        Step = CellSize + gapBetweenCells;

        float totalWidth = (columns * Step) - gapBetweenCells;
        float totalHeight = (rows * Step) - gapBetweenCells;

        GridStartX = -(totalWidth / 2f) + (CellSize / 2f);
        GridStartY = (totalHeight / 2f) - (CellSize / 2f);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                float posX = GridStartX + (c * Step);
                float posY = GridStartY - (r * Step);
                Vector3 spawnPosition = new Vector3(posX, posY, 0f);

                GameObject newCell = Instantiate(cellPrefab, spawnPosition,
                    Quaternion.identity, transform);
                newCell.name = $"Cell_{r}_{c}";

                SpriteRenderer renderer = newCell.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    if (themeManager != null && themeManager.activeTheme != null)
                        renderer.sprite = themeManager.activeTheme.emptyCellSprite;
                    renderer.color = cellColor;
                }
            }
        }
    }

    public void UpdateTheme(ThemeData theme)
    {
        foreach (Transform child in transform)
        {
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = theme.emptyCellSprite;
                renderer.color = cellColor;
            }
        }
    }

    public Vector3 GetCellWorldPosition(int x, int y)
    {
        float posX = GridStartX + x * Step;
        float posY = GridStartY - y * Step;
        return transform.TransformPoint(new Vector3(posX, posY, 0f));
    }

    public bool WorldToGrid(Vector3 worldPos, out int x, out int y)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        float gx = (localPos.x - GridStartX) / Step;
        float gy = (GridStartY - localPos.y) / Step;
        x = Mathf.RoundToInt(gx);
        y = Mathf.RoundToInt(gy);
        return x >= 0 && x < columns && y >= 0 && y < rows;
    }
}
