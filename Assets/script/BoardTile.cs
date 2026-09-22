using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to EVERY tile GameObject on the board (the ones you place
/// manually in the Editor).
///
/// Wiring per tile:
///  - Single-layer tile (nothing on top of it): leave "Covered By Tile" empty.
///  - FRONT tile of a stack (the one on top): leave "Covered By Tile" empty too — it's always tappable.
///  - BACK tile of a stack (the one underneath): drag the FRONT tile into its "Covered By Tile" field.
///
/// No need to ever touch Button > Interactable — the blocking is handled
/// entirely in code below, based on whether the covering tile is still on the board.
/// </summary>
[RequireComponent(typeof(Button))]
public class BoardTile : MonoBehaviour
{
    [Header("What this tile shows")]
    public TileIconType type;

    [Header("Coverage (only fill this in on a BACK/underneath tile)")]
    [Tooltip("If another tile sits on top of THIS one, drag that tile here. While it's still active on the board, taps on this tile are ignored.")]
    [SerializeField] private BoardTile coveredByTile;

    private Button button;
    private GameBoardManager boardManager;

    private void Awake()
    {
        button = GetComponent<Button>();
        boardManager = GetComponentInParent<GameBoardManager>();
        button.onClick.AddListener(HandleTap);
    }

    private void HandleTap()
    {
        if (boardManager == null) return;

        if (boardManager.IsInputBlocked())
            return;

        // Still covered by another tile in front of it — ignore the tap entirely.
        if (coveredByTile != null && coveredByTile.gameObject.activeInHierarchy)
            return;

        boardManager.OnTileTapped(this);
    }

    /// <summary>Called by GameBoardManager once this tile has flown into the tray. Hides this tile from the board.</summary>
    public void RemoveFromBoard()
    {
        gameObject.SetActive(false);
        // Nothing else to do here — any tile that had "Covered By Tile" pointing
        // at this one will automatically become tappable, since HandleTap()
        // checks coveredByTile.gameObject.activeInHierarchy every time.
    }
}