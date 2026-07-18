using UnityEngine;
using System.Collections.Generic;

public class BlockDragHandler : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 smallScale;
    private Vector3 offset;
    private Camera mainCamera;
    private bool isDragging = false;
    private bool isTouchDevice = false;

    private BlockPiece blockPiece;
    private GridManager gridManager;
    private List<GameObject> ghostCells = new List<GameObject>();
    private List<SpriteRenderer> ghostRenderers = new List<SpriteRenderer>();
    private Vector2Int[] shapeOffsets;
    private float precomputedShapeCenterCol;
    private float precomputedShapeCenterRow;
    private int shapeOffsetCount;
    private int hoverOriginX, hoverOriginY;
    private bool isValidPlacement = false;
    private SpriteRenderer[] cellRenderers;
    private Color pieceColor = Color.white;
    private int lastHoverX = int.MinValue;
    private int lastHoverY = int.MinValue;
    private bool lastGhostValid;
    private Color ghostValidColor;
    public int slotIndex = -1;
    private float gridTopY;
    private float slotY;

    [Header("Scaling")]
    public float spawnScale = 0.33f;

    [Header("Lift")]
    public float liftHeight = 0.8f;
    public float maxExtraLift = 0.6f;

    [Header("Ghost")]
    public float ghostAlpha = 0.5f;

    [Header("Hit Area")]
    public float hitPadding = 0.2f;

    void Start()
    {
        mainCamera = Camera.main;
        startPosition = transform.localPosition;
        smallScale = Vector3.one * spawnScale;
        blockPiece = GetComponent<BlockPiece>();
        gridManager = GridManager.Instance;
        cellRenderers = GetComponentsInChildren<SpriteRenderer>();

        isTouchDevice = Input.touchSupported && Application.platform != RuntimePlatform.WindowsEditor
            && Application.platform != RuntimePlatform.WindowsPlayer
            && Application.platform != RuntimePlatform.OSXEditor
            && Application.platform != RuntimePlatform.OSXPlayer;

        if (cellRenderers != null && cellRenderers.Length > 0 && cellRenderers[0] != null)
            pieceColor = cellRenderers[0].color;

        slotY = transform.position.y;

        // Precompute shape bounds so we don't recalculate every frame
        if (blockPiece != null && blockPiece.CurrentPattern.cellOffsets != null)
        {
            shapeOffsets = blockPiece.CurrentPattern.cellOffsets;
            shapeOffsetCount = shapeOffsets.Length;
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < shapeOffsetCount; i++)
            {
                int ox = shapeOffsets[i].x;
                int oy = shapeOffsets[i].y;
                if (ox < minX) minX = ox;
                if (ox > maxX) maxX = ox;
                if (oy < minY) minY = oy;
                if (oy > maxY) maxY = oy;
            }
            precomputedShapeCenterCol = (minX + maxX) * 0.5f;
            precomputedShapeCenterRow = (minY + maxY) * 0.5f;
        }

        GridManager gm = gridManager != null ? gridManager : GridManager.Instance;
        if (gm != null && gm.gridSystem != null)
        {
            float cs = gm.gridSystem.CellSize;
            float gsy = gm.gridSystem.GridStartY;
            gridTopY = gsy + cs / 2f;
        }
    }

    void CreateGhostCells()
    {
        DestroyGhostCells();

        BlockPiece bp = blockPiece != null ? blockPiece : GetComponent<BlockPiece>();
        if (bp == null || bp.blockCellPrefab == null) return;

        GridManager gm = gridManager != null ? gridManager : GridManager.Instance;
        if (gm == null || gm.gridSystem == null) return;

        for (int i = 0; i < shapeOffsetCount; i++)
        {
            GameObject cell = Instantiate(bp.blockCellPrefab, gm.gridSystem.transform);
            SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.color = Color.clear;
            ghostCells.Add(cell);
            ghostRenderers.Add(renderer);
        }

        ghostValidColor = new Color(pieceColor.r, pieceColor.g, pieceColor.b, ghostAlpha);
        lastHoverX = int.MinValue;
        lastHoverY = int.MinValue;
        lastGhostValid = false;
    }

    void DestroyGhostCells()
    {
        for (int i = 0; i < ghostCells.Count; i++)
        {
            if (ghostCells[i] != null) Destroy(ghostCells[i]);
        }
        ghostCells.Clear();
        ghostRenderers.Clear();
        isValidPlacement = false;
        lastHoverX = int.MinValue;
        lastHoverY = int.MinValue;
        lastGhostValid = false;
    }

    void UpdateGhostPosition()
    {
        if (ghostCells.Count == 0) return;

        GridManager gm = gridManager != null ? gridManager : GridManager.Instance;
        if (gm == null || gm.gridSystem == null) return;

        // Fast snap: inline GetSnappedWorldPosition with precomputed shape center
        Vector3 localPos = gm.gridSystem.transform.InverseTransformPoint(transform.position);
        float centerCol = (localPos.x - gm.gridSystem.GridStartX) / gm.gridSystem.Step;
        float centerRow = (gm.gridSystem.GridStartY - localPos.y) / gm.gridSystem.Step;
        int newHoverX = Mathf.RoundToInt(centerCol - precomputedShapeCenterCol);
        int newHoverY = Mathf.RoundToInt(centerRow - precomputedShapeCenterRow);

        // Skip if snapped grid position hasn't changed
        if (newHoverX == lastHoverX && newHoverY == lastHoverY) return;

        hoverOriginX = newHoverX;
        hoverOriginY = newHoverY;
        lastHoverX = newHoverX;
        lastHoverY = newHoverY;

        // Validate placement
        isValidPlacement = true;
        bool anyInBounds = false;
        for (int i = 0; i < shapeOffsetCount; i++)
        {
            int x = hoverOriginX + shapeOffsets[i].x;
            int y = hoverOriginY + shapeOffsets[i].y;
            if (!gm.IsInBounds(x, y))
            {
                isValidPlacement = false;
            }
            else
            {
                anyInBounds = true;
                if (gm.IsOccupied(x, y))
                    isValidPlacement = false;
            }
        }

        if (!anyInBounds)
            isValidPlacement = false;

        // Update ghost cell positions (localPosition avoids TransformPoint native call)
        float gsx = gm.gridSystem.GridStartX;
        float gsy = gm.gridSystem.GridStartY;
        float step = gm.gridSystem.Step;
        for (int i = 0; i < ghostCells.Count; i++)
        {
            float posX = gsx + (hoverOriginX + shapeOffsets[i].x) * step;
            float posY = gsy - (hoverOriginY + shapeOffsets[i].y) * step;
            ghostCells[i].transform.localPosition = new Vector3(posX, posY, 0f);
        }

        // Only update colors when validity state changes
        if (isValidPlacement != lastGhostValid)
        {
            lastGhostValid = isValidPlacement;
            Color c = isValidPlacement ? ghostValidColor : Color.clear;
            for (int i = 0; i < ghostRenderers.Count; i++)
            {
                if (ghostRenderers[i] != null)
                    ghostRenderers[i].color = c;
            }
        }
    }

    public static event System.Action<GameObject> PiecePlaced;

    void PlaceOnGrid()
    {
        BlockPiece bp = blockPiece != null ? blockPiece : GetComponent<BlockPiece>();
        GridManager gm = gridManager != null ? gridManager : GridManager.Instance;
        if (!isValidPlacement || bp == null || gm == null) return;

        gm.PlaceBlock(shapeOffsets, hoverOriginX, hoverOriginY);

        Vector3 originWorldPos = gm.gridSystem.GetCellWorldPosition(hoverOriginX, hoverOriginY);

        // Use the stored pivot offset to align offset (0,0) with the grid origin
        transform.position = originWorldPos - bp.PivotOffset;

        transform.SetParent(gm.gridSystem.transform);
        transform.localScale = Vector3.one;

        isDragging = false;
        enabled = false;

        PiecePlaced?.Invoke(gameObject);
    }

    bool IsPointerOverPiece(Vector3 worldPos)
    {
        if (slotIndex >= 0)
        {
            float screenY = mainCamera.WorldToScreenPoint(worldPos).y;
            if (screenY > Screen.height * 0.3f) return false;

            float zoneWidth = Screen.width / 3f;
            int zone = Mathf.FloorToInt(Input.mousePosition.x / zoneWidth);
            return zone == slotIndex;
        }

        if (cellRenderers == null) return false;
        foreach (SpriteRenderer sr in cellRenderers)
        {
            if (sr == null) continue;
            Bounds expanded = sr.bounds;
            expanded.Expand(hitPadding);
            if (expanded.Contains(worldPos))
                return true;
        }
        return false;
    }

    Vector3 GetPointerWorldPos()
    {
        Vector2 screenPos;
        if (isTouchDevice && Input.touchCount > 0)
            screenPos = Input.GetTouch(0).position;
        else
            screenPos = Input.mousePosition;

        if (float.IsNaN(screenPos.x) || float.IsInfinity(screenPos.x)
            || float.IsNaN(screenPos.y) || float.IsInfinity(screenPos.y))
            return transform.position;

        return mainCamera.ScreenToWorldPoint(new Vector3(
            screenPos.x, screenPos.y,
            -mainCamera.transform.position.z));
    }

    bool IsPointerPressed()
    {
        if (isTouchDevice && Input.touchCount > 0)
        {
            TouchPhase phase = Input.GetTouch(0).phase;
            return phase == TouchPhase.Began || phase == TouchPhase.Moved
                || phase == TouchPhase.Stationary;
        }
        return Input.GetMouseButton(0);
    }

    bool IsPointerDown()
    {
        if (isTouchDevice && Input.touchCount > 0)
            return Input.GetTouch(0).phase == TouchPhase.Began;
        return Input.GetMouseButtonDown(0);
    }

    bool IsPointerUp()
    {
        if (isTouchDevice && Input.touchCount > 0)
        {
            TouchPhase phase = Input.GetTouch(0).phase;
            return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
        }
        return Input.GetMouseButtonUp(0);
    }

    void Update()
    {
        if (mainCamera == null) return;

        if (isDragging)
        {
            DragUpdate();
            return;
        }

        if (!IsPointerDown()) return;

        Vector3 worldPos = GetPointerWorldPos();
        if (IsPointerOverPiece(worldPos))
        {
            isDragging = true;
            transform.localScale = Vector3.one;
            offset = transform.position - worldPos;
            offset.y += liftHeight;
            CreateGhostCells();
        }
    }

    void DragUpdate()
    {
        Vector3 worldPos = GetPointerWorldPos();

        if (IsPointerPressed())
        {
            Vector3 targetPos = worldPos + offset;
            float t = Mathf.Clamp01((targetPos.y - slotY) / (gridTopY - slotY));
            targetPos.y += t * maxExtraLift;
            transform.position = targetPos;
            UpdateGhostPosition();
        }

        if (IsPointerUp())
        {
            isDragging = false;
            if (isValidPlacement)
                PlaceOnGrid();
            else
            {
                transform.localPosition = startPosition;
                transform.localScale = smallScale;
            }
            DestroyGhostCells();
        }
    }
}
