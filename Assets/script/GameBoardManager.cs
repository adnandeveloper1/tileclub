using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// The real (playable) board manager — put this on your 'GameBoard' parent
/// GameObject. Every BoardTile child automatically finds this via
/// GetComponentInParent, so no manual wiring needed per-tile.
///
/// Handles: tapped tile → flies to tray → checks for 3-match → clears with
/// shrink animation → checks win (board empty) / lose (tray full).
/// </summary>
public class GameBoardManager : MonoBehaviour
{
    [Header("Icon lookup")]
    [SerializeField] private TileIconDatabase iconDatabase;

    [Header("Tray (7 slot Images — start with Sprite = None, Alpha = 0)")]
    [SerializeField] private Image[] traySlots;

    [Header("Timing")]
    [SerializeField] private float flyDuration = 0.3f;
    [SerializeField] private float matchClearDelay = 0.25f;
    [SerializeField] private float shrinkDuration = 0.2f;

    [Header("Events (wire these to your popups)")]
    public UnityEvent onLevelComplete;
    public UnityEvent onGameOver;

    [Header("Level start animation")]
    [SerializeField] private bool playStartAnimation = true;
    [SerializeField] private float startAnimationDuration = 0.45f;
    [SerializeField] private float startAnimationStagger = 0.035f;
    [SerializeField] private float startAnimationOffset = 180f;
    [SerializeField] private float tutorialPopupDelay = 1f;

    private Vector3[] slotHomePositions;
    private readonly Queue<BoardTile> pendingTiles = new Queue<BoardTile>();
    private readonly HashSet<BoardTile> queuedTiles = new HashSet<BoardTile>();
    private int remainingTileCount;
    private bool levelEnded;
    private bool startAnimationPlaying;
    private bool processingTrayQueue;
    private Rect lastCanvasRect;

    private void Awake()
    {
        slotHomePositions = new Vector3[traySlots.Length];
        for (int i = 0; i < traySlots.Length; i++)
        {
            traySlots[i].sprite = null;
            SetAlpha(traySlots[i], 0f);
        }

        // Count every tile GameObject on the board (single-layer + stacked).
        remainingTileCount = GetComponentsInChildren<BoardTile>(true).Length;
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        CaptureSlotHomePositions();

        if (playStartAnimation)
            StartCoroutine(PlayLevelStartAnimation());
    }

    private void LateUpdate()
    {
        Rect canvasRect = ((RectTransform)transform.root).rect;
        if (canvasRect != lastCanvasRect)
        {
            Canvas.ForceUpdateCanvases();
            CaptureSlotHomePositions();
        }
    }

    private void CaptureSlotHomePositions()
    {
        for (int i = 0; i < traySlots.Length; i++)
            slotHomePositions[i] = traySlots[i].rectTransform.position;

        lastCanvasRect = ((RectTransform)transform.root).rect;
    }

    /// <summary>Called by a BoardTile when the player taps it.</summary>
    public void OnTileTapped(BoardTile tile)
    {
        if (levelEnded || startAnimationPlaying) return;

        if (!queuedTiles.Add(tile))
            return;

        pendingTiles.Enqueue(tile);
        if (!processingTrayQueue)
            StartCoroutine(ProcessTrayQueue());
    }

    private IEnumerator ProcessTrayQueue()
    {
        processingTrayQueue = true;

        while (pendingTiles.Count > 0 && !levelEnded)
        {
            BoardTile tile = pendingTiles.Dequeue();
            queuedTiles.Remove(tile);

            if (tile == null || !tile.gameObject.activeInHierarchy)
                continue;

            int slotIndex = FindNextEmptySlot();
            if (slotIndex == -1)
            {
                levelEnded = true;
                onGameOver?.Invoke();
                break;
            }

            yield return StartCoroutine(SendToTray(tile, slotIndex));
        }

        pendingTiles.Clear();
        queuedTiles.Clear();
        processingTrayQueue = false;
    }

    private IEnumerator PlayLevelStartAnimation()
    {
        startAnimationPlaying = true;

        BoardTile[] boardTiles = GetComponentsInChildren<BoardTile>(true);
        List<Vector3> tilePositions = new List<Vector3>(boardTiles.Length);
        List<Vector3> tileScales = new List<Vector3>(boardTiles.Length);

        foreach (BoardTile boardTile in boardTiles)
        {
            tilePositions.Add(boardTile.transform.localPosition);
            tileScales.Add(boardTile.transform.localScale);
            boardTile.transform.localPosition += Vector3.down * startAnimationOffset;
            boardTile.transform.localScale = Vector3.zero;
        }

        GameObject trayRow = GameObject.Find("ribbon");
        Vector3 trayRowPosition = Vector3.zero;
        bool hasTrayRow = trayRow != null;
        if (hasTrayRow)
        {
            trayRowPosition = trayRow.transform.localPosition;
            trayRow.transform.localPosition += Vector3.down * startAnimationOffset;
        }

        Image brownTray = FindBrownTrayImage();
        if (brownTray != null)
        {
            brownTray.type = Image.Type.Filled;
            brownTray.fillMethod = Image.FillMethod.Horizontal;
            brownTray.fillOrigin = 0;
            brownTray.fillAmount = 0f;
        }

        float elapsed = 0f;
        float totalDuration = startAnimationDuration + Mathf.Max(0, boardTiles.Length - 1) * startAnimationStagger;
        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            if (hasTrayRow)
            {
                float trayProgress = Mathf.Clamp01(elapsed / startAnimationDuration);
                trayRow.transform.localPosition = Vector3.Lerp(trayRowPosition - Vector3.down * startAnimationOffset, trayRowPosition, EaseOutBack(trayProgress));
            }

            if (brownTray != null)
            {
                float revealProgress = Mathf.Clamp01(elapsed / (startAnimationDuration + 6f * startAnimationStagger));
                brownTray.fillAmount = Mathf.Ceil(revealProgress * 7f) / 7f;
            }

            for (int i = 0; i < boardTiles.Length; i++)
            {
                float tileProgress = Mathf.Clamp01((elapsed - i * startAnimationStagger) / startAnimationDuration);
                float easedProgress = EaseOutBack(tileProgress);
                boardTiles[i].transform.localPosition = Vector3.Lerp(tilePositions[i] - Vector3.down * startAnimationOffset, tilePositions[i], easedProgress);
                boardTiles[i].transform.localScale = Vector3.LerpUnclamped(Vector3.zero, tileScales[i], easedProgress);
            }

            yield return null;
        }

        if (hasTrayRow)
            trayRow.transform.localPosition = trayRowPosition;

        if (brownTray != null)
            brownTray.fillAmount = 1f;

        for (int i = 0; i < boardTiles.Length; i++)
        {
            boardTiles[i].transform.localPosition = tilePositions[i];
            boardTiles[i].transform.localScale = tileScales[i];
        }

        yield return new WaitForSeconds(tutorialPopupDelay);

        GameObject tutorialPopup = FindInactiveSceneObject("Tutorial popup");
        if (tutorialPopup != null)
            tutorialPopup.SetActive(true);

        startAnimationPlaying = false;
    }

    private Image FindBrownTrayImage()
    {
        GameObject trayObject = GameObject.Find("mini boxes image ");
        return trayObject != null ? trayObject.GetComponent<Image>() : null;
    }

    private GameObject FindInactiveSceneObject(string objectName)
    {
        foreach (GameObject sceneObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (sceneObject.name == objectName && sceneObject.scene.IsValid())
                return sceneObject;
        }

        return null;
    }

    private float EaseOutBack(float progress)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        float adjusted = progress - 1f;
        return 1f + c3 * adjusted * adjusted * adjusted + c1 * adjusted * adjusted;
    }

    private int FindNextEmptySlot()
    {
        for (int i = 0; i < traySlots.Length; i++)
            if (traySlots[i].sprite == null)
                return i;
        return -1;
    }

    private IEnumerator SendToTray(BoardTile tile, int slotIndex)
    {
        Image slot = traySlots[slotIndex];
        Sprite sprite = iconDatabase.GetSprite(tile.type);

        slot.sprite = sprite;
        SetAlpha(slot, 1f);
        slot.rectTransform.position = tile.transform.position;

        tile.RemoveFromBoard();
        remainingTileCount--;

        Vector3 start = slot.rectTransform.position;
        Vector3 end = slotHomePositions[slotIndex];
        float t = 0f;
        while (t < flyDuration)
        {
            t += Time.deltaTime;
            slot.rectTransform.position = Vector3.Lerp(start, end, t / flyDuration);
            yield return null;
        }
        slot.rectTransform.position = end;

        yield return StartCoroutine(CheckForMatch());

        if (levelEnded) yield break;

        if (remainingTileCount <= 0)
        {
            levelEnded = true;
            onLevelComplete?.Invoke();
        }
        else if (FindNextEmptySlot() == -1)
        {
            levelEnded = true;
            onGameOver?.Invoke();
        }
    }

    private IEnumerator CheckForMatch()
    {
        var groups = new Dictionary<Sprite, List<int>>();
        for (int i = 0; i < traySlots.Length; i++)
        {
            Sprite s = traySlots[i].sprite;
            if (s == null) continue;
            if (!groups.ContainsKey(s)) groups[s] = new List<int>();
            groups[s].Add(i);
        }

        foreach (var kvp in groups)
        {
            if (kvp.Value.Count >= 3)
            {
                yield return new WaitForSeconds(matchClearDelay);

                // kvp.Value is already sorted ascending (we scanned slots 0..N in order).
                int a = kvp.Value[0];
                int b = kvp.Value[1];
                int c = kvp.Value[2];

                // Slide/exchange whatever tile is sitting between them out of
                // the way, and slide the matching tile into its place, so
                // all 3 matches end up next to each other. If they're
                // already adjacent, no swap happens and they just clear in place.
                if (b != a + 1)
                {
                    if (c == a + 1) c = b; // the tile we're about to displace is actually our other match
                    yield return StartCoroutine(SwapSlots(a + 1, b));
                    b = a + 1;
                }
                if (c != a + 2)
                {
                    yield return StartCoroutine(SwapSlots(a + 2, c));
                    c = a + 2;
                }

                List<int> toClear = new List<int> { a, b, c };

                float elapsed = 0f;
                while (elapsed < shrinkDuration)
                {
                    elapsed += Time.deltaTime;
                    float scale = Mathf.Lerp(1f, 0f, elapsed / shrinkDuration);
                    foreach (int idx in toClear)
                        traySlots[idx].rectTransform.localScale = Vector3.one * scale;
                    yield return null;
                }

                foreach (int idx in toClear)
                {
                    traySlots[idx].sprite = null;
                    SetAlpha(traySlots[idx], 0f);
                    traySlots[idx].rectTransform.localScale = Vector3.one;
                    traySlots[idx].rectTransform.position = slotHomePositions[idx];
                }

                CompactTraySlots();
                yield break;
            }
        }
    }

    /// <summary>
    /// Animates the tiles at these two slot positions swapping places
    /// (crossing paths), then re-points the array so traySlots[i] always
    /// refers to whatever is now sitting at slotHomePositions[i].
    /// </summary>
    private IEnumerator SwapSlots(int indexA, int indexB)
    {
        if (indexA == indexB) yield break;

        Image imgA = traySlots[indexA];
        Image imgB = traySlots[indexB];

        Vector3 startA = imgA.rectTransform.position;
        Vector3 startB = imgB.rectTransform.position;

        float duration = 0.25f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            imgA.rectTransform.position = Vector3.Lerp(startA, startB, p);
            imgB.rectTransform.position = Vector3.Lerp(startB, startA, p);
            yield return null;
        }
        imgA.rectTransform.position = startB;
        imgB.rectTransform.position = startA;

        traySlots[indexA] = imgB;
        traySlots[indexB] = imgA;
    }

    private void CompactTraySlots()
    {
        List<Sprite> remaining = new List<Sprite>();
        foreach (Image slot in traySlots)
            if (slot.sprite != null)
                remaining.Add(slot.sprite);

        for (int i = 0; i < traySlots.Length; i++)
        {
            if (i < remaining.Count)
            {
                traySlots[i].sprite = remaining[i];
                SetAlpha(traySlots[i], 1f);
                traySlots[i].rectTransform.position = slotHomePositions[i];
            }
            else
            {
                traySlots[i].sprite = null;
                SetAlpha(traySlots[i], 0f);
            }
        }
    }

    private void SetAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }
}