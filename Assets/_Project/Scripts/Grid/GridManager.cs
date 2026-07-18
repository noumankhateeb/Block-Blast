using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    public GridSystem gridSystem;

    private bool[,] grid;
    private int rows, cols;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            rows = gridSystem.rows;
            cols = gridSystem.columns;
            grid = new bool[cols, rows];
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetGrid()
    {
        grid = new bool[cols, rows];

        if (gridSystem != null)
        {
            List<Transform> toDestroy = new List<Transform>();
            foreach (Transform child in gridSystem.transform)
            {
                if (child.GetComponent<BlockPiece>() != null)
                    toDestroy.Add(child);
            }
            foreach (Transform t in toDestroy)
                Destroy(t.gameObject);
        }
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < cols && y >= 0 && y < rows;
    }

    public bool IsOccupied(int x, int y)
    {
        if (!IsInBounds(x, y)) return true;
        return grid[x, y];
    }

    public bool CanPlaceAnywhere(Vector2Int[] offsets)
    {
        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
            {
                bool valid = true;
                foreach (Vector2Int offset in offsets)
                {
                    int nx = x + offset.x;
                    int ny = y + offset.y;
                    if (!IsInBounds(nx, ny) || grid[nx, ny])
                    { valid = false; break; }
                }
                if (valid) return true;
            }
        return false;
    }

    public bool CanPlace(Vector2Int[] offsets, int originX, int originY)
    {
        foreach (Vector2Int offset in offsets)
        {
            int x = originX + offset.x;
            int y = originY + offset.y;
            if (!IsInBounds(x, y) || grid[x, y]) return false;
        }
        return true;
    }

    public void PlaceBlock(Vector2Int[] offsets, int originX, int originY)
    {
        foreach (Vector2Int offset in offsets)
        {
            int x = originX + offset.x;
            int y = originY + offset.y;
            if (IsInBounds(x, y))
            {
                grid[x, y] = true;
            }
        }
    }

    public int CheckAndClearLines()
    {
        List<int> fullRows = new List<int>();
        List<int> fullCols = new List<int>();

        for (int y = 0; y < rows; y++)
        {
            bool full = true;
            for (int x = 0; x < cols; x++)
                if (!grid[x, y]) { full = false; break; }
            if (full) fullRows.Add(y);
        }

        for (int x = 0; x < cols; x++)
        {
            bool full = true;
            for (int y = 0; y < rows; y++)
                if (!grid[x, y]) { full = false; break; }
            if (full) fullCols.Add(x);
        }

        if (fullRows.Count == 0 && fullCols.Count == 0) return 0;

        HashSet<Vector2Int> toClear = new HashSet<Vector2Int>();
        foreach (int y in fullRows)
            for (int x = 0; x < cols; x++)
                toClear.Add(new Vector2Int(x, y));
        foreach (int x in fullCols)
            for (int y = 0; y < rows; y++)
                toClear.Add(new Vector2Int(x, y));

        foreach (var pos in toClear)
            grid[pos.x, pos.y] = false;

        DestroyVisualCellsAt(toClear);

        int totalCells = toClear.Count;
        int totalLines = fullRows.Count + fullCols.Count;

                ScoreManager.Instance?.AddLineClearScore(totalLines, totalCells);
return totalLines;
    }

    private void DestroyVisualCellsAt(HashSet<Vector2Int> positions)
    {
        if (gridSystem == null) return;

        List<GameObject> toDestroy = new List<GameObject>();

        foreach (Transform piece in gridSystem.transform)
        {
            if (piece.GetComponent<BlockPiece>() == null) continue;

            foreach (Transform cell in piece)
            {
                if (gridSystem.WorldToGrid(cell.position, out int gx, out int gy))
                {
                    if (positions.Contains(new Vector2Int(gx, gy)))
                        toDestroy.Add(cell.gameObject);
                }
            }
        }

        foreach (GameObject go in toDestroy)
            Destroy(go);

        List<Transform> emptyPieces = new List<Transform>();
        foreach (Transform piece in gridSystem.transform)
        {
            if (piece.GetComponent<BlockPiece>() != null && piece.childCount == 0)
                emptyPieces.Add(piece);
        }
        foreach (Transform piece in emptyPieces)
            Destroy(piece.gameObject);
    }

    public Vector3 GetSnappedWorldPosition(Vector2Int[] offsets, Vector3 worldCenter, out int originX, out int originY)
    {
        originX = 0;
        originY = 0;

        Vector3 localPos = gridSystem.transform.InverseTransformPoint(worldCenter);
        float centerCol = (localPos.x - gridSystem.GridStartX) / gridSystem.Step;
        float centerRow = (gridSystem.GridStartY - localPos.y) / gridSystem.Step;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (Vector2Int offset in offsets)
        {
            if (offset.x < minX) minX = offset.x;
            if (offset.x > maxX) maxX = offset.x;
            if (offset.y < minY) minY = offset.y;
            if (offset.y > maxY) maxY = offset.y;
        }

        float shapeCenterCol = (minX + maxX) / 2f;
        float shapeCenterRow = (minY + maxY) / 2f;

        originX = Mathf.RoundToInt(centerCol - shapeCenterCol);
        originY = Mathf.RoundToInt(centerRow - shapeCenterRow);

        return gridSystem.GetCellWorldPosition(originX, originY);
    }
}
