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
        new Color(1f, 0.18f, 0.18f),
        new Color(1f, 0.6f, 0f),
        new Color(1f, 0.9f, 0.1f),
        new Color(0.3f, 0.85f, 0.3f),
        new Color(0f, 0.8f, 0.8f),
        new Color(0.2f, 0.4f, 1f),
        new Color(0.6f, 0.2f, 1f),
        new Color(1f, 0.2f, 0.6f),
        new Color(0.7f, 1f, 0.2f),
        new Color(0f, 0.7f, 0.5f),
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

        int guaranteedIndex = Random.Range(0, spawnSlots.Length);

        for (int i = 0; i < spawnSlots.Length; i++)
        {
            ShapePattern selectedShape;

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
