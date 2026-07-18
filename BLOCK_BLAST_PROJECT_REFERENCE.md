# Block Blast — Complete Unity Project Reference

> **Generated:** 2026-06-29  
> **Unity Version:** 6000.4.10f1  
> **Render Pipeline:** Universal Render Pipeline (URP) 2D  
> **Active Input Handler:** Input Manager (Legacy) — `activeInputHandler: 1`  
> **Scripting Backend (Android):** IL2CPP  
> **Target Platform:** Android (ARM64, Min SDK 25)

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Architecture & Data Flow](#2-architecture--data-flow)
3. [Folder Structure](#3-folder-structure)
4. [Sprite Assets & Import Settings](#4-sprite-assets--import-settings)
5. [Scriptable Objects](#5-scriptable-objects)
6. [Prefabs](#6-prefabs)
7. [Scene Hierarchy](#7-scene-hierarchy)
8. [Scripts (Full Code)](#8-scripts-full-code)
9. [System Flow: Step by Step](#9-system-flow-step-by-step)
10. [Grid Sizing Math](#10-grid-sizing-math)
11. [Shape Library (41 Shapes)](#11-shape-library-41-shapes)
12. [Key Values & Constants](#12-key-values--constants)
13. [Known Issues & Edge Cases](#13-known-issues--edge-cases)
14. [How to Build for Android](#14-how-to-build-for-android)
15. [AI Instructions](#15-ai-instructions)

---

## 1. Project Overview

Block Blast block-puzzle game. 8×8 grid, randomly colored block shapes, drag-and-drop with ghost preview, line clearing, game-over detection with restart.

### Current State
- **Working:** 8×8 grid rendering, theme management, 41 shape types, block spawning in 3 slots, drag-and-drop with lift effect (0.8→1.4 normalized), ghost preview (60% opacity, piece-colored, no flash), snap-to-grid placement (PivotOffset-based), line clearing, game-over detection with restart button, auto camera sizing, touch + mouse input
- **Not Implemented (MVP):** Scoring system / score display, top bar UI (logo, score text, high score), sound effects (placement, line clear, game over), theme switching at runtime (infrastructure exists but no UI), `blockSprites[]` array in ThemeData is unused — all blocks use Block.png regardless of theme
- **Performance Optimizations Applied:**
  - Physics2DRaycaster removed from Main Camera (was doing expensive per-frame raycasts)
  - EventSystem InputModule runtime hack removed (GridSystem no longer destroys/adds input modules)
  - Post-processing disabled on camera (`m_RenderPostProcessing: 0`)
  - URP shadows disabled, HDR disabled, Volume framework set to Via Scripting
  - Drag handler optimized: ghost colors pre-computed, early exit on unchanged hover, color updates only on validity change
  - Shape center precomputed in Start() to avoid recalculating bounds per frame

### Bugs
| # | Bug | File | Severity |
|---|-----|------|----------|
| 1 | `GridSystem.UpdateTheme()` iterates ALL children including placed pieces — changes their sprite to `emptyCellSprite` and color to `cellColor`. Only grid cells should be updated. Intended for theme-switching feature which is not yet exposed via UI. | `GridSystem.cs:83-93` | Low (not triggerable without theme-switching UI) |
| 2 | `availableThemes[0]` has null entry in scene — `ThemeManager` Inspector shows 1 element with "None (Theme Data)". | ThemeManager Inspector | Medium — remove the empty entry in Inspector |
| 3 | `Background` GameObject under GameRoot has no SpriteRenderer or any component — it's an empty GameObject. | Scene Hierarchy | Low |
| 4 | `ThemeManager.ApplyTheme()` called in `Awake()` before `Camera.main` or `gridSystem` are guaranteed ready — early call is effectively a no-op for grid cells. `GridSystem.Start()` calls `GenerateGrid()` which then reads `themeManager.activeTheme` and applies it correctly. | `ThemeManager.cs:19` | Low (second application in `GenerateGrid` is correct) |
| 5 | Division by zero in lift if `gridTopY == slotY` (not possible with current layout Y=-2.8 vs ~1.666). | `BlockDragHandler.cs:323` | Edge case |
| 6 | No scoring or score display — the game has no progression feedback. | N/A | Feature missing |
| 7 | No sound effects — placement, line clear, and game over are silent. | N/A | Feature missing |

### Key Technical Decisions
- **Input:** Input Manager (Legacy) only — `Input.mousePosition`, `Input.GetMouseButtonDown/0/Up`, `Input.GetTouch()`, `Input.touchCount`. Input System package is installed but unused.
- **EventSystem:** `InputSystemUIInputModule` remains on EventSystem (harmless, no runtime cleanup needed). StandaloneInputModule is NOT added — the GameOver restart button uses `SceneManager.LoadScene` triggered by `Button.onClick` which works with either input module. UI raycasting does not conflict with game input.
- **Grid State:** `GridManager` (singleton) owns `bool[,]` occupancy; `GridSystem` owns visual generation + grid math.
- **Placement:** Uses `BlockPiece.PivotOffset` (stored during `SetupPiece`) to correctly align ANY shape to the grid origin — handles shapes without a cell at offset `(0,0)`.
- **Ghost:** Cells spawn invisible (`Color.clear`), ghost color pre-computed once on creation. Position only updated when snapped grid cell changes (cached `lastHoverX/Y`). Colors only updated when validity state flips (cached `lastGhostValid`). No per-frame allocations.
- **Lift:** Base 0.8 on click, smooth normalized ramp to +0.6 at grid top.
- **Performance:** Shape center precomputed in `Start()` to inline snap calculation. `Update()` returns early when idle (no drag, no click). `UpdateGhostPosition()` returns early if hover cell hasn't changed. Ghost color only set when validity flips. NaN/Inf guard prevents log spam.
- **Camera:** `CameraFitter` script replaces `Physics2DRaycaster`. Auto-sizes camera so board fills width on any phone. Post-processing disabled on camera.
- **URP Settings:** Shadows disabled, HDR disabled, Volume framework set to Via Scripting (2).
- **Game Over:** Checked after each placement AND after each new-set spawn.

---

## 2. Architecture & Data Flow

### Component Dependency Graph

```
ThemeManager (singleton on GameRoot)
  ├── boardRenderer  → Board SpriteRenderer
  └── gridSystem     → GridSystem (calls UpdateTheme)

GridSystem (on GridSystem, child of Board)
  ├── blockSpawner   → BlockSpawner (calls SpawnNewSet)
  └── GridManager (singleton, same GameObject)
        └── gridSystem → back-reference to GridSystem

BlockSpawner (on GameRoot)
  ├── shapeData      → GameShapes.asset (41 shapes)
  ├── blockPiecePrefab → BlockPiece.prefab
  ├── spawnSlots[3]  → Slot_0/1/2 Transforms
  └── subscribes to BlockDragHandler.PiecePlaced

BlockPiece.prefab
  ├── BlockPiece (script) → CurrentPattern, PivotOffset, spawns BlockCells
  ├── BoxCollider2D (Is Trigger) → hit area
  └── BlockDragHandler (script) → drag/ghost/placement

CameraFitter (on Main Camera) → sets orthographic size

GameOverScreen (created dynamically) → has Canvas + RESTART button
```

### Data Flow: Startup
```
1. ThemeManager.Awake() → ApplyTheme (camera color, board sprite, grid cells)
2. CameraFitter.Start()  → orthographicSize = boardWidth / (2 * aspect)
3. GridManager.Awake()   → Instance, grid[8,8] initialized
4. GridSystem.Start()    → GenerateGrid → SpawnNewSet
5. BlockSpawner.SpawnNewSet() → 3 random BlockPieces in slots, colored
```

### Data Flow: Drag & Place
```
1. Update() checks IsPointerDown() → GetPointerWorldPos() → IsPointerOverPiece()
2. Zone tap: bottom 30% screen, 3 zones → match slotIndex
3. On drag start: scale=1, offset+y+=liftHeight, CreateGhostCells() (clear)
4. DragUpdate() each frame: position+lift, UpdateGhostPosition()
5. Ghost: GetSnappedWorldPosition → CanPlace → position cells → show/hide
6. On release: valid → PlaceOnGrid() (PivotOffset formula) → PiecePlaced event
7. Invalid: snap back to slot, scale=0.33
8. BlockSpawner.OnPiecePlaced: remove piece → CheckAndClearLines → check game-over
```

---

## 3. Folder Structure

```
Assets/
├── _Project/
│   ├── Prefabs/
│   │   ├── BlockCell.prefab       # Block cell (Sorting Order 10)
│   │   ├── BlockPiece.prefab      # Draggable piece (BlockPiece + BoxCollider + BlockDragHandler)
│   │   └── GridCell.prefab        # Grid placeholder (Sorting Order 1)
│   ├── Scripts/
│   │   ├── Blocks/
│   │   │   ├── BlockDragHandler.cs  # Drag/ghost/placement/PivotOffset alignment
│   │   │   ├── BlockPiece.cs        # SetupPiece, CurrentPattern, PivotOffset
│   │   │   ├── BlockShape.cs        # ShapePattern struct + BlockShape SO
│   │   │   └── BlockSpawner.cs      # Spawn/color/game-over/event cleanup
│   │   ├── Core/
│   │   │   ├── CameraFitter.cs      # Auto camera size
│   │   │   ├── ThemeData.cs         # Theme SO definition
│   │   │   └── ThemeManager.cs      # Singleton theme applier
│   │   ├── Grid/
│   │   │   ├── GridSystem.cs        # Grid generation, math, cellColor
│   │   │   └── GridManager.cs       # Occupancy, placement, line clearing, snapping
│   │   └── UI/
│   │       └── GameOverScreen.cs    # Self-contained Canvas UI
│   ├── Settings/
│   │   ├── Shapes/
│   │   │   └── GameShapes.asset     # 41 shape patterns
│   │   └── Themes/
│   │       └── Theme_Default.asset  # Cyan background
│   └── Sprites/
│       ├── Block.png                # 1024×1024 @ 256 PPU
│       ├── Board.png                # 1024×1024 @ 256 PPU
│       └── Placeholder.png          # 1024×1024 @ 256 PPU
├── Scenes/
│   └── SampleScene.unity            # Main scene
└── InputSystem_Actions.inputactions # Installed but UNUSED
```

---

## 4. Sprite Assets & Import Settings

| File | Resolution | PPU | World Size (scale 1) |
|------|-----------|-----|---------------------|
| Block.png | 1024×1024 | 256 | 4 × 4 units |
| Board.png | 1024×1024 | 256 | 4 × 4 units |
| Placeholder.png | 1024×1024 | 256 | 4 × 4 units |

All: Sprite (2D), Single, Pivot Center, Bilinear, No Mip Maps, sRGB.

Prefab scale: `(0.114, 0.114, 1)` → world size: `4 × 0.114 = 0.456 units`.

---

## 5. Scriptable Objects

### 5.1 GameShapes.asset
- 41 ShapePattern entries (see [Shape Library](#11-shape-library-41-shapes))

### 5.2 Theme_Default.asset
- `themeName`: "Default", `boardSprite`: Board.png, `emptyCellSprite`: Placeholder.png
- `cameraBackgroundColor`: Cyan (R:0, G:0.887, B:1, A:1)

---

## 6. Prefabs

### 6.1 GridCell.prefab
- Scale: `(0.114, 0.114, 1)`, Sprite: Placeholder_0, Sorting Order: 1
- World size: **0.456 units**

### 6.2 BlockCell.prefab
- Scale: `(0.114, 0.114, 1)`, Sprite: Block_0, Sorting Order: 10

### 6.3 BlockPiece.prefab
- Components: Transform, BlockPiece (BlockCell → BlockCell.prefab), BoxCollider2D (Is Trigger), BlockDragHandler (`spawnScale: 0.33`, `liftHeight: 0.8`, `maxExtraLift: 0.6`, `ghostAlpha: 0.6`)

---

## 7. Scene Hierarchy

```
Main Camera          (0, 0, -10) [Camera: Ortho, CameraFitter, URP Camera]
Global Light 2D      (0, 0, 0)
GameRoot             (0, 0, 0)   [ThemeManager, BlockSpawner]
├── Background       (0, 0, 10)  [EMPTY]
├── Board            (0, 0, 1)   [SpriteRenderer: Board.png]
│   └── GridSystem   (0, 0, 0)  [GridSystem, GridManager]
│       └── [64 cells spawned at runtime]
└── SpawnPositions   (0, 0, 0)
    ├── Slot_0       (-1.2, -2.8, 0)
    ├── Slot_1       (0, -2.8, 0)
    └── Slot_2       (1.2, -2.8, 0)
```

### Camera
- Orthographic, Size: auto-set by CameraFitter to `4.2 / (2 * cam.aspect)`

---

## 8. Scripts (Full Code)

### 8.1 BlockShape.cs

```csharp
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
```

**Key:** `cellOffsets` use Y+ = down convention. `preferredColor` unused at runtime.

---

### 8.2 BlockPiece.cs

```csharp
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
        PivotOffset = new Vector3(-totalWidth / 2f + size / 2f,
                                   totalHeight / 2f - size / 2f, 0f);

        foreach (Vector2Int offset in pattern.cellOffsets)
        {
            GameObject cell = Instantiate(blockCellPrefab, transform);
            float posX = offset.x * step;
            float posY = -offset.y * step;
            cell.transform.localPosition = new Vector3(posX, posY, 0f) + PivotOffset;
        }

        // Auto-resize BoxCollider2D to match shape bounds
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null && transform.childCount > 0)
        {
            Bounds bounds = new Bounds(transform.GetChild(0).localPosition, Vector3.zero);
            for (int i = 1; i < transform.childCount; i++)
                bounds.Encapsulate(transform.GetChild(i).localPosition);
            bounds.Expand(size * 0.5f);
            box.size = bounds.size;
            box.offset = bounds.center;
        }
    }
}
```

**Key:**
- `PivotOffset` — the centering offset. Stored for use in `PlaceOnGrid()` to correctly align ANY shape to grid origin, even shapes without a cell at offset `(0,0)`.
- Children created in order of `cellOffsets` array. Child[i] corresponds to `offsets[i]`.

---

### 8.3 BlockDragHandler.cs

```csharp
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
    public float ghostAlpha = 0.6f;

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

        // Update ghost cell positions
        for (int i = 0; i < ghostCells.Count; i++)
        {
            Vector3 cellPos = gm.gridSystem.GetCellWorldPosition(
                hoverOriginX + shapeOffsets[i].x,
                hoverOriginY + shapeOffsets[i].y);
            ghostCells[i].transform.position = cellPos;
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
```

**Key Details:**
- **Performance:** `Update()` returns early when idle (no drag, no click). `DragUpdate()` runs only when dragging. No `ScreenToWorldPoint` for idle handlers.
- **NaN Guard:** Prevents "Screen position out of view frustum" log spam on Android.
- **Ghost Flash Fix:** Ghost cells created with `Color.clear` — no frame-1 flash at screen center.
- **PivotOffset Placement:** `transform.position = originWorldPos - bp.PivotOffset` — mathematically correct for all 41 shapes.
- **Zone Tap:** Bottom 30% of screen, split into 3 equal horizontal zones.

---

### 8.4 BlockSpawner.cs

```csharp
using UnityEngine;
using System.Collections.Generic;

public class BlockSpawner : MonoBehaviour
{
    [Header("References")]
    public BlockShape shapeData;
    public GameObject blockPiecePrefab;

    [Header("Spawn Positions")]
    public Transform[] spawnSlots;

    [Header("Colors")]
    public Color[] colorPalette = new Color[]
    {
        new Color(1f, 0.18f, 0.18f),   // Red
        new Color(1f, 0.6f, 0f),       // Orange
        new Color(1f, 0.9f, 0.1f),     // Yellow
        new Color(0.3f, 0.85f, 0.3f),  // Green
        new Color(0f, 0.8f, 0.8f),     // Cyan
        new Color(0.2f, 0.4f, 1f),     // Blue
        new Color(0.6f, 0.2f, 1f),     // Purple
        new Color(1f, 0.2f, 0.6f),     // Pink
        new Color(0.7f, 1f, 0.2f),     // Lime
        new Color(0f, 0.7f, 0.5f),     // Teal
    };

    private List<GameObject> activePieces = new List<GameObject>();

    private void Start()
    {
        BlockDragHandler.PiecePlaced += OnPiecePlaced;
    }

    private void OnDestroy()
    {
        BlockDragHandler.PiecePlaced -= OnPiecePlaced;
    }

    private void OnPiecePlaced(GameObject piece)
    {
        activePieces.Remove(piece);

        GridManager.Instance.CheckAndClearLines();

        if (activePieces.Count > 0 && !AnyPieceCanBePlaced())
        {
            GameOver();
            return;
        }

        if (activePieces.Count == 0)
        {
            SpawnNewSet();
            if (!AnyPieceCanBePlaced())
                GameOver();
        }
    }

    private bool AnyPieceCanBePlaced()
    {
        GridManager gm = GridManager.Instance;
        if (gm == null) return false;

        foreach (GameObject piece in activePieces)
        {
            BlockPiece bp = piece.GetComponent<BlockPiece>();
            if (bp == null || bp.CurrentPattern.cellOffsets == null) continue;
            if (gm.CanPlaceAnywhere(bp.CurrentPattern.cellOffsets))
                return true;
        }
        return false;
    }

    private void GameOver()
    {
        foreach (GameObject piece in activePieces)
        {
            if (piece == null) continue;
            BlockDragHandler handler = piece.GetComponent<BlockDragHandler>();
            if (handler != null) handler.enabled = false;
        }
        activePieces.Clear();

        GameOverScreen.Show();
    }

    public void SpawnNewSet()
    {
        activePieces.Clear();

        if (shapeData == null || shapeData.shapes == null || shapeData.shapes.Length == 0) return;

        // Find all shapes that can currently be placed on the grid
        List<ShapePattern> validShapes = new List<ShapePattern>();
        GridManager gm = GridManager.Instance;
        if (gm != null)
        {
            foreach (ShapePattern shape in shapeData.shapes)
            {
                if (shape.cellOffsets != null && gm.CanPlaceAnywhere(shape.cellOffsets))
                {
                    validShapes.Add(shape);
                }
            }
        }

        // Pick a random slot to receive the guaranteed valid piece
        int guaranteedIndex = Random.Range(0, spawnSlots.Length);

        for (int i = 0; i < spawnSlots.Length; i++)
        {
            ShapePattern selectedShape;

            // Guarantee at least one placeable shape if possible
            if (i == guaranteedIndex && validShapes.Count > 0)
            {
                selectedShape = validShapes[Random.Range(0, validShapes.Count)];
            }
            else
            {
                int randomIndex = Random.Range(0, shapeData.shapes.Length);
                selectedShape = shapeData.shapes[randomIndex];
            }

            GameObject pieceGo = Instantiate(blockPiecePrefab, spawnSlots[i]);
            pieceGo.transform.localPosition = Vector3.zero;
            pieceGo.transform.localScale = Vector3.one * 0.33f;

            BlockPiece piece = pieceGo.GetComponent<BlockPiece>();
            if (piece != null)
            {
                piece.SetupPiece(selectedShape, 0.456f, 0.02f);

                Color randomColor = colorPalette[Random.Range(0, colorPalette.Length)];
                foreach (Transform child in piece.transform)
                {
                    SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.color = randomColor;
                    }
                }
            }

            BlockDragHandler dragHandler = pieceGo.GetComponent<BlockDragHandler>();
            if (dragHandler != null)
                dragHandler.slotIndex = i;

            activePieces.Add(pieceGo);
        }
    }
}
```

**Key:** Subscribes/unsubscribes to `BlockDragHandler.PiecePlaced` in `Start()`/`OnDestroy()` for proper scene-reload cleanup.

---

### 8.5 GridSystem.cs

```csharp
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

    // Exposed grid calculation data (set during GenerateGrid)
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
```

**Key:**
- `cellColor` — applied to all grid cells (default dark gray-blue). Tweakable in Inspector.
- Camera fitting removed — handled by `CameraFitter` on Main Camera instead.
- InputModule hack removed — `InputSystemUIInputModule` stays on EventSystem (harmless), no runtime cleanup code.
- **⚠️ Bug:** `UpdateTheme()` iterates `foreach (Transform child in transform)` — this includes placed `BlockPiece` containers (which are parented to `gridSystem.transform` in `PlaceOnGrid()`). It overwrites their sprites to `emptyCellSprite` and color to `cellColor`, corrupting placed blocks visually. Only grid placeholder cells (those WITHOUT `BlockPiece` component) should be affected. Currently low severity because no theme-switching UI exists at runtime.

---

### 8.6 GridManager.cs

```csharp
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
                grid[x, y] = true;
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

        // Clean up empty piece containers
        List<Transform> emptyPieces = new List<Transform>();
        foreach (Transform piece in gridSystem.transform)
        {
            if (piece.GetComponent<BlockPiece>() != null && piece.childCount == 0)
                emptyPieces.Add(piece);
        }
        foreach (Transform piece in emptyPieces)
            Destroy(piece.gameObject);
    }

    public Vector3 GetSnappedWorldPosition(Vector2Int[] offsets, Vector3 worldCenter,
        out int originX, out int originY)
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
```

**Key:** `DestroyVisualCellsAt` also cleans up empty piece containers (pieces whose all cells were cleared). `GetSnappedWorldPosition()` removed — snap logic inlined into `BlockDragHandler.UpdateGhostPosition()` using precomputed shape center.

---

### 8.7 GameOverScreen.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour
{
    private static GameOverScreen instance;
    private GameObject panel;

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
        RectTransform btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(200, 55);
        btnRect.anchoredPosition = new Vector2(0, -30);

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
```

**Key:** Self-contained — creates its own Canvas, no scene setup required. `Show()` is static.

---

### 8.8 ThemeManager.cs

```csharp
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

        if (Camera.main != null)
            Camera.main.backgroundColor = activeTheme.cameraBackgroundColor;

        if (boardRenderer != null)
            boardRenderer.sprite = activeTheme.boardSprite;

        if (gridSystem != null)
            gridSystem.UpdateTheme(activeTheme);
    }
}
```

---

### 8.9 ThemeData.cs

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Block Blast/Theme")]
public class ThemeData : ScriptableObject
{
    public string themeName;
    [Header("Board Sprites")]
    public Sprite boardSprite;
    [Header("Placeholder Sprites")]
    public Sprite emptyCellSprite;
    [Header("Block Sprites (By Piece Type)")]
    public Sprite[] blockSprites;      // UNUSED
    [Header("UI & Background")]
    public Color cameraBackgroundColor = Color.black;
}
```

---

### 8.10 CameraFitter.cs

```csharp
using UnityEngine;

public class CameraFitter : MonoBehaviour
{
    public float boardWidth = 4.2f;

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        cam.orthographicSize = boardWidth / (2f * cam.aspect);
    }
}
```

**Key:** Sets orthographic size so the board (4.2 units wide) fills the screen width on any device. Attach to Main Camera.

---

## 9. System Flow: Step by Step

### 9.1 Startup
| Step | Action | Script |
|------|--------|--------|
| 1 | ThemeManager.Awake() → camera color, board sprite, grid cells | ThemeManager |
| 2 | CameraFitter.Start() → orthographic size auto-set | CameraFitter |
| 3 | GridManager.Awake() → Instance + grid[8,8] | GridManager |
| 4 | GridSystem.Start() → GenerateGrid, SpawnNewSet | GridSystem |
| 5 | GenerateGrid() → 64 cells with theme sprite + cellColor tint | GridSystem |
| 6 | SpawnNewSet() → 3 random pieces, colored, slotIndex set | BlockSpawner |

### 9.2 Drag & Place
| Step | Action | Script |
|------|--------|--------|
| 1 | IsPointerDown? + zone/bounds check | BlockDragHandler.Update() |
| 2 | Start drag: scale=1, offset+=0.8, CreateGhostCells() (pre-compute ghost color, clear) | BlockDragHandler |
| 3 | DragUpdate(): follow finger, normalized lift | BlockDragHandler |
| 4 | UpdateGhostPosition(): inline snap using precomputed shape center, early exit if hover unchanged, validate placement, update ghost positions, update colors only on validity flip | BlockDragHandler |
| 5 | Release: valid → PlaceOnGrid() via PivotOffset formula | BlockDragHandler |
| 6 | Piece disabled, parented to gridSystem, PiecePlaced event | BlockDragHandler |
| 7 | Invalid → snap back to slot, scale=0.33 | BlockDragHandler |
| 8 | Ghost destroyed | BlockDragHandler |
| 9 | BlockSpawner.OnPiecePlaced → remove, CheckAndClearLines, game-over | BlockSpawner |

### 9.3 Line Clearing
| Step | Action |
|------|--------|
| 1 | CheckAndClearLines() → scan rows + columns for full lines |
| 2 | Build HashSet of positions to clear |
| 3 | Set grid[x,y] = false for each |
| 4 | DestroyVisualCellsAt() → destroy cell GameObjects at those grid positions |
| 5 | Destroy empty piece containers (pieces with 0 children left) |

### 9.4 Game Over
| Step | Action |
|------|--------|
| 1 | After placement: remaining pieces cannot fit OR new set cannot fit |
| 2 | Disable all remaining drag handlers, clear activePieces |
| 3 | GameOverScreen.Show() → creates Canvas, "GAME OVER" + "RESTART" |
| 4 | Click RESTART → SceneManager.LoadScene → full reload |

---

## 10. Grid Sizing Math

| Parameter | Value | Formula |
|-----------|-------|---------|
| CellSize | 0.456 units | 1024/256 × 0.114 |
| gapBetweenCells | 0.02 | Inspector |
| Step | 0.476 units | 0.456 + 0.02 |
| totalWidth | 3.788 units | 8 × 0.476 − 0.02 |
| totalHeight | 3.788 units | same |
| Board size | 4 × 4 units | |
| Margin per side | 0.106 units | (4 − 3.788) / 2 |
| GridStartX | -1.666 | −3.788/2 + 0.456/2 |
| GridStartY | 1.666 | 3.788/2 − 0.456/2 |

BlockPiece SetupPiece receives `cellSize: 0.456f, gap: 0.02f` — must match GridSystem values.

---

## 11. Shape Library (41 Shapes)

41 shapes in `GameShapes.asset`. Offsets use Y+ = down (row 0 = top).

### 1 Block
| # | Name | Offsets | Cells |
|---|------|---------|-------|
| 1 | Single | (0,0) | 1 |

### 2 Blocks
| # | Name | Offsets | Cells |
|---|------|---------|-------|
| 2 | 2_Horizontal | (0,0),(1,0) | 2 |
| 3 | 2_Vertical | (0,0),(0,1) | 2 |
| 4 | 2_Diagonal_DownRight | (0,0),(1,1) | 2 |
| 5 | 2_Diagonal_UpRight | (0,1),(1,0) | 2 |

### 3 Blocks
| # | Name | Offsets | Cells |
|---|------|---------|-------|
| 6 | 3_Horizontal | (0,0),(1,0),(2,0) | 3 |
| 7 | 3_Vertical | (0,0),(0,1),(0,2) | 3 |
| 8 | 3L_UpLeft | (0,0),(0,1),(1,1) | 3 |
| 9 | 3L_UpRight | (1,0),(0,1),(1,1) | 3 |
| 10 | 3L_DownLeft | (0,0),(1,0),(0,1) | 3 |
| 11 | 3L_DownRight | (0,0),(1,0),(1,1) | 3 |
| 12 | 3_Diagonal_DownRight | (0,0),(1,1),(2,2) | 3 |
| 13 | 3_Diagonal_UpRight | (0,2),(1,1),(2,0) | 3 |

### 4 Blocks
| # | Name | Offsets | Cells |
|---|------|---------|-------|
| 14 | 2x2_Square | (0,0),(1,0),(0,1),(1,1) | 4 |
| 15 | 4_Horizontal | (0,0),(1,0),(2,0),(3,0) | 4 |
| 16 | 4_Vertical | (0,0),(0,1),(0,2),(0,3) | 4 |
| 17 | 4L_UpLeft | (0,0),(0,1),(0,2),(1,2) | 4 |
| 18 | 4L_UpRight | (1,0),(1,1),(0,2),(1,2) | 4 |
| 19 | 4L_DownLeft | (0,0),(1,0),(0,1),(0,2) | 4 |
| 20 | 4L_DownRight | (0,0),(1,0),(1,1),(1,2) | 4 |
| 21 | 4T_Up | (0,0),(1,0),(2,0),(1,1) | 4 |
| 22 | 4T_Down | (1,0),(0,1),(1,1),(2,1) | 4 |
| 23 | 4T_Left | (1,0),(0,1),(1,1),(1,2) | 4 |
| 24 | 4T_Right | (0,0),(0,1),(1,1),(0,2) | 4 |
| 25 | 4S_Horizontal | (0,0),(1,0),(1,1),(2,1) | 4 |
| 26 | 4S_Vertical | (0,0),(0,1),(1,1),(1,2) | 4 |
| 27 | 4S_ReverseHorizontal | (1,0),(2,0),(0,1),(1,1) | 4 |
| 28 | 4S_ReverseVertical | (1,0),(0,1),(1,1),(0,2) | 4 |
| 29 | 4Z_Horizontal | (1,0),(2,0),(0,1),(1,1) | 4 |
| 30 | 4Z_Vertical | (1,0),(0,1),(1,1),(0,2) | 4 |
| 31 | 4Z_ReverseHorizontal | (0,0),(1,0),(1,1),(2,1) | 4 |
| 32 | 4Z_ReverseVertical | (0,0),(0,1),(1,1),(1,2) | 4 |

### 5 Blocks
| # | Name | Offsets | Cells |
|---|------|---------|-------|
| 33 | 5_Horizontal | (0,0),(1,0),(2,0),(3,0),(4,0) | 5 |
| 34 | 5_Vertical | (0,0),(0,1),(0,2),(0,3),(0,4) | 5 |
| 35 | 5L_UpLeft | (0,0),(0,1),(0,2),(1,2),(2,2) | 5 |
| 36 | 5L_UpRight | (2,0),(2,1),(0,2),(1,2),(2,2) | 5 |
| 37 | 5L_DownLeft | (0,0),(1,0),(2,0),(0,1),(0,2) | 5 |
| 38 | 5L_DownRight | (0,0),(1,0),(2,0),(2,1),(2,2) | 5 |

### Large Shapes
| # | Name | Offsets | Cells |
|---|------|---------|-------|
| 39 | 2x3_Rectangle | (0,0),(1,0),(0,1),(1,1),(0,2),(1,2) | 6 |
| 40 | 3x2_Rectangle | (0,0),(1,0),(2,0),(0,1),(1,1),(2,1) | 6 |
| 41 | 3x3_Square | 9 offsets (0,0) to (2,2) | 9 |

---

## 12. Key Values & Constants

| Value | Location | Purpose |
|-------|----------|---------|
| Camera: `boardWidth = 4.2` | CameraFitter Inspector | Auto camera size |
| `spawnScale = 0.33` | BlockPiece prefab | Slot scale |
| `liftHeight = 0.8` | BlockPiece prefab | Click lift |
| `maxExtraLift = 0.6` | BlockPiece prefab | Ramped lift at grid top |
| `ghostAlpha = 0.6` | BlockPiece prefab | Ghost opacity |
| `hitPadding = 0.2` | BlockPiece prefab | Tap hit area expansion |
| `gapBetweenCells = 0.02` | GridSystem Inspector | Grid gap |
| `cellColor = (0.5, 0.5, 0.55)` | GridSystem Inspector | Grid cell tint |
| `SetupPiece(0.456f, 0.02f)` | BlockSpawner hardcoded | Cell size, gap |
| Slots at Y = -2.8 | Slot Transforms | Spawn position |
| Zone threshold: 0.3 | BlockDragHandler | Bottom 30% of screen |
| Zone width: Screen.width / 3 | BlockDragHandler | 3 equal zones |
| 10 colors in colorPalette | BlockSpawner Inspector | Random piece colors |
| Board scale: (1,1,1) | Board Transform | No scaling |

---

## 13. Known Issues & Edge Cases

| # | Issue | Details | Severity |
|---|-------|---------|----------|
| 1 | `GridSystem.UpdateTheme()` corrupts placed blocks | Iterates ALL children of `gridSystem.transform`, including placed `BlockPiece` containers. Changes their sprite to `emptyCellSprite` and color to `cellColor`. Cannot be triggered without theme-switching UI (which doesn't exist). Fix: skip children that have `BlockPiece` component. | Medium (dormant) |
| 2 | `availableThemes[0]` null entry in Inspector | ThemeManager has an empty `availableThemes[0]` element set to "None (Theme Data)". Should be removed. | Medium |
| 3 | `Background` GameObject is empty | Has no SpriteRenderer, Image, or any component. Harmless placeholder. | Low |
| 4 | `ThemeManager.ApplyTheme()` in Awake() is premature | `Camera.main` may be null, `gridSystem` reference may not be set yet. However, `GridSystem.Start()` → `GenerateGrid()` re-reads `themeManager.activeTheme` and applies it correctly, so the early call is redundant but harmless. | Low |
| 5 | No scoring / score display | Game has no sense of progression. Cells placed and lines cleared grant no points. | Feature missing (MVP gap) |
| 6 | No sound effects | All interactions are silent. | Feature missing |
| 7 | Division by zero in lift if `gridTopY == slotY` | Lift normalization `t = (targetPos.y - slotY) / (gridTopY - slotY)` at `BlockDragHandler.cs:323` would divide by zero if slot and grid are at same Y. Not possible with current layout (slot Y = -2.8, grid top ~1.666). | Edge case |
| 8 | `IsPointerOverPiece()` zone tap uses `Input.mousePosition.x` not `worldPos` | Zone detection reads `Input.mousePosition` directly instead of converting the touch/mouse position from the `worldPos` that was already computed. Works correctly in practice because `IsPointerOverPiece` is called with the already-computed `worldPos` but then recalculates screen position. Minor inconsistency. | Low |
| 9 | No Android back button handling | Pressing back on Android does nothing. Should show a confirmation or exit. | Feature missing |
| 10 | `preferredColor` field in ShapePattern is unused | Each ShapePattern has a `preferredColor` field but BlockSpawner ignores it and assigns random colors from `colorPalette[]`. The field exists in the SO data but is dead code. | Low |
| 11 | Global Light 2D is active but unnecessary | The scene has a Global Light 2D that forces a per-sprite light pass in the 2D Renderer. For a flat puzzle game with no normal maps, this adds GPU overhead with zero visual impact. Can be disabled or removed. | Low |

---

## 14. How to Build for Android

1. Open in Unity 6000.4.10f1
2. File → Build Profiles → select Android profile
3. Click **Build**, choose output (e.g., `build/BlockBlast.apk`)
4. First build ~1 hr (shader cache), subsequent much faster

---

## 15. AI Instructions

### Architecture Pattern
- **GridSystem** = visual + math (coordinates, cell generation)
- **GridManager** = state (occupancy, line clearing)
- **BlockSpawner** = lifecycle (spawn, track, game-over)
- **BlockDragHandler** = interaction (drag, ghost, place)
- **BlockPiece** = data container (CurrentPattern, PivotOffset)
- **GameOverScreen** = self-contained UI
- **CameraFitter** = auto camera sizing

### Key Relationships
1. `GridManager.Instance` — singleton, accessed from BlockDragHandler and BlockSpawner
2. `BlockDragHandler.PiecePlaced` — static event, subscribed only by BlockSpawner
3. `BlockPiece.PivotOffset` — stored during SetupPiece, used in PlaceOnGrid for correct alignment. Formula: `PivotOffset = new Vector3(-totalWidth/2 + cellSize/2, totalHeight/2 - cellSize/2, 0)`. This centers the shape's bounding box around the GameObject's origin, which then aligns to the grid origin in `PlaceOnGrid()` via `transform.position = originWorldPos - PivotOffset`.
4. Slot index (0,1,2) = zone index = bottom 30% screen split into 3 zones
5. Random colors per piece, NOT from shape's `preferredColor` — color is picked from `colorPalette[]` array in BlockSpawner
6. Shape offset convention: Y+ = DOWN (row 0 = top of grid). This affects PivotOffset calculation (positive Y in offsets increases row index downward).

### Performance Patterns (Maintain These)
- **Drag handler:** Only 3 active `BlockDragHandler` instances exist at any time. Idle handlers early-return on `IsPointerDown()` check. During drag, `UpdateGhostPosition()` early-exits if hover cell hasn't changed. Ghost color set only on validity state flip — no per-frame `SpriteRenderer.color` sets.
- **Ghost cells:** Pre-compute colors in `CreateGhostCells()`. Cache last hover position and last validity state. Use separate `ghostRenderers` list to avoid `GetComponent<SpriteRenderer>()` per frame.
- **Renderer/Camera:** `Physics2DRaycaster` removed from camera. Post-processing disabled (`m_RenderPostProcessing: 0`). URP shadows/HDR disabled. Volume framework set to Via Scripting.
- **Input:** No EventSystem input module conflicts — `InputSystemUIInputModule` left in place (no runtime cleanup). Game uses `Input.GetMouseButton()` directly in `Update()`.

### If Adding Features
- **Scoring:** Add ScoreManager singleton. Call `AddScore(cellCount)` from `BlockDragHandler.PlaceOnGrid()` (1 point per cell placed), call `AddScore(lineBonus)` from `GridManager.CheckAndClearLines()` (e.g. 1 line = 10, 2 = 30, 3 = 60, 4 = 100). Display via a Canvas Text in a top bar.
- **Sound:** Add AudioManager singleton with `AudioSource`. Call `PlayClip()` from `PlaceOnGrid()` (placement sound), `CheckAndClearLines()` (line clear sound, return value > 0), and `GameOverScreen.Show()` (game over sound).
- **More shapes:** Add entries to GameShapes.asset. All offsets use Y+ = down convention. Each shape must have at least one cell. No shape may have duplicate offsets.
- **Theme switching:** Populate `availableThemes` array in ThemeManager Inspector, fix `UpdateTheme()` to skip `BlockPiece` children, then call `ApplyTheme()` from UI.
- **Top UI (score/logo):** Create as separate Canvas (like GameOverScreen), keep always active. Place above grid, below game-over overlay.
- **High score:** Use `PlayerPrefs` to persist high score between sessions.
- **Animation:** Add tweening for piece snap-to-grid on placement, cell destruction animation on line clear, and slot piece entrance animation on new set.
- **Android back button:** Add `if (Input.GetKeyDown(KeyCode.Escape))` handling — currently unhandled.

### File Dependency Map
```
BlockShape.cs  ←  BlockSpawner.cs  ←  GridSystem.cs
                     ↓
               BlockPiece.cs  ←  BlockDragHandler.cs  →  BlockSpawner.cs (event)
                                    ↓
                              GridManager.cs  ←  BlockDragHandler.cs
                              GridManager.cs  ←  BlockSpawner.cs (game-over)
                                    ↓
                              GridSystem.cs  (grid math, cell position)
                              
CameraFitter.cs  →  Main Camera (auto size)
ThemeManager.cs  →  GridSystem.cs (UpdateTheme)
GameOverScreen.cs  →  SceneManager (restart)
```
