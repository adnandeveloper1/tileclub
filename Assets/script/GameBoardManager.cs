using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
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

    [Header("Match effect")]
    [SerializeField] private bool playMatchBurst = true;
    [SerializeField] private MatchBurstEffect matchBurstEffect;

    [Header("Events (wire these to your popups)")]
    public UnityEvent onLevelComplete;
    public UnityEvent onGameOver;
    [SerializeField] private GameObject gameLossPanel;
    [SerializeField] private GameManager gameManager;
    [Tooltip("Leave 0 to read the number from the scene name, for example lvl1 = level 1.")]
    [SerializeField] private int levelNumberOverride;

    [Header("Win wisdom animation")]
    [SerializeField] private RectTransform sunCircle;
    [SerializeField] private TMP_Text wisdomText;
    [SerializeField] private float wisdomAnimationDuration = 0.6f;
    [SerializeField] private float sunStartScale = 0.4f;
    [SerializeField] private float sunOvershootScale = 1.3f;

    [Header("Coins")]
    [Tooltip("Drag the coin TMP text from this level's Canvas here.")]
    [SerializeField] private TMP_Text coinText;
    [Tooltip("Optional: effect that flies coin icons into the coin bar after a level reward.")]
    [SerializeField] private CoinFlyEffect coinFlyEffect;

    [Header("Level reward bar")]
    [Tooltip("Drag the colored Image that fills inside the reward bar.")]
    [SerializeField] private Image levelRewardBar;
    [Tooltip("Color used by the filled part of the level reward bar.")]
    [SerializeField] private Color levelRewardFillColor = Color.white;
    [SerializeField] private int totalRewardBarSegments = 4;
    [SerializeField] private float rewardBarFillDuration = 0.35f;

    [Header("Level start animation")]
    [SerializeField] private bool playStartAnimation = true;
    [SerializeField] private float startAnimationDuration = 0.45f;
    [SerializeField] private float startAnimationStagger = 0.035f;
    [SerializeField] private float startAnimationOffset = 180f;
    [SerializeField] private float tutorialPopupDelay = 1f;

    [Header("Level win exit animation (tray leaves — opposite of entrance)")]
    [Tooltip("If on, the tray row + brick reveal plays in reverse (sliding UP and away, bricks disappearing) right before the Level Complete event fires.")]
    [SerializeField] private bool playWinExitAnimation = true;
    [Tooltip("Leave 0 to reuse Start Animation Duration above.")]
    [SerializeField] private float winExitAnimationDuration = 0f;
    [Tooltip("Leave 0 to reuse Start Animation Offset above.")]
    [SerializeField] private float winExitAnimationOffset = 0f;

    private Vector3[] slotHomePositions;
    private readonly Queue<BoardTile> pendingTiles = new Queue<BoardTile>();
    private readonly HashSet<BoardTile> queuedTiles = new HashSet<BoardTile>();
    private int remainingTileCount;
    private bool levelEnded;
    private bool startAnimationPlaying;
    private bool processingTrayQueue;
    private Rect lastCanvasRect;
    private bool skipTutorialOnReplay;
    private bool inputBlocked;
    private Vector3 finalSunScale;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;

        UpdateCoinText();

        if (sunCircle != null)
            finalSunScale = sunCircle.localScale;

        SetupLevelRewardBar();

        if (gameLossPanel == null)
            gameLossPanel = FindInactiveSceneObject("GameLossPanel");

        if (gameLossPanel != null)
            gameLossPanel.SetActive(false);

        slotHomePositions = new Vector3[traySlots.Length];
        for (int i = 0; i < traySlots.Length; i++)
        {
            traySlots[i].sprite = null;
            SetAlpha(traySlots[i], 0f);
        }

        // Count every tile GameObject on the board (single-layer + stacked).
        remainingTileCount = GetComponentsInChildren<BoardTile>(true).Length;
    }

    private void OnEnable()
    {
        if (gameManager != null)
            gameManager.OnCoinsChanged += HandleCoinsChanged;
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.OnCoinsChanged -= HandleCoinsChanged;
    }

    private void HandleCoinsChanged(int totalCoins)
    {
        if (coinText != null)
            coinText.text = totalCoins.ToString();
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        CaptureSlotHomePositions();
        skipTutorialOnReplay = ButtonActions.ConsumeReplayTutorialSkip();
        SetBoardInputBlocked(false);

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
        if (inputBlocked || levelEnded || startAnimationPlaying) return;

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
                ShowGameLoss();
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

        if (!skipTutorialOnReplay)
        {
            yield return new WaitForSeconds(tutorialPopupDelay);

            GameObject tutorialPopup = FindInactiveSceneObject("Tutorial popup");
            if (tutorialPopup != null)
                tutorialPopup.SetActive(true);
        }

        startAnimationPlaying = false;
    }

    /// <summary>
    /// Mirror image of PlayLevelStartAnimation: the tray row slides UP and
    /// away (instead of in from below), and the brown tray's brick reveal
    /// empties back out (instead of filling up) — brick by brick, in reverse.
    /// Runs once, right before the Level Complete event fires.
    /// </summary>
    private IEnumerator PlayLevelCompleteExitAnimation()
    {
        float duration = winExitAnimationDuration > 0f ? winExitAnimationDuration : startAnimationDuration;
        float offset = winExitAnimationOffset > 0f ? winExitAnimationOffset : startAnimationOffset;

        GameObject trayRow = GameObject.Find("ribbon");
        bool hasTrayRow = trayRow != null;
        Vector3 trayRowStartPosition = hasTrayRow ? trayRow.transform.localPosition : Vector3.zero;
        Vector3 trayRowExitPosition = trayRowStartPosition + Vector3.up * offset;

        Image brownTray = FindBrownTrayImage();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = EaseInBack(progress);

            if (hasTrayRow)
                trayRow.transform.localPosition = Vector3.LerpUnclamped(trayRowStartPosition, trayRowExitPosition, eased);

            // Bricks disappear in the opposite order/direction from how they filled in.
            if (brownTray != null)
                brownTray.fillAmount = Mathf.Floor((1f - progress) * 7f) / 7f;

            yield return null;
        }

        if (hasTrayRow)
            trayRow.transform.localPosition = trayRowExitPosition;

        if (brownTray != null)
            brownTray.fillAmount = 0f;
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

    /// <summary>Mirror of EaseOutBack — starts slow, overshoots backward, then accelerates away. Feels like the reverse of the entrance.</summary>
    private float EaseInBack(float progress)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return c3 * progress * progress * progress - c1 * progress * progress;
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

            if (playWinExitAnimation)
                yield return StartCoroutine(PlayLevelCompleteExitAnimation());

            TriggerLevelCompletePopup();
        }
        else if (FindNextEmptySlot() == -1)
        {
            ShowGameLoss();
        }
    }

    private void TriggerLevelCompletePopup()
    {
        SetBoardInputBlocked(true);

        if (gameManager == null)
            gameManager = GameManager.Instance;

        bool rewardGranted = false;
        int wisdomBefore = 0;
        int wisdomAfter = 0;
        int coinsAfter = gameManager != null ? gameManager.Coins : 0;
        if (gameManager != null)
        {
            int levelNumber = GetLevelNumber();
            rewardGranted = gameManager.TryAwardWisdomForLevel(levelNumber, out wisdomBefore, out wisdomAfter);
            bool coinsRewarded = gameManager.TryAwardCoinsForLevel(levelNumber, out _, out coinsAfter);

            if (coinsRewarded && coinFlyEffect != null)
                coinFlyEffect.PlayBurst();
        }

        // Let win-panel listeners activate the panel before changing its UI.
        onLevelComplete?.Invoke();

        if (rewardGranted)
            StartCoroutine(PlayWisdomWinAnimationNextFrame(wisdomBefore, wisdomAfter));
        else if (gameManager != null && wisdomText != null)
            wisdomText.text = gameManager.Wisdom.ToString();

        if (coinText != null)
            coinText.text = coinsAfter.ToString();

        if (gameManager != null)
            StartCoroutine(UpdateLevelRewardBarNextFrame(gameManager.LastRewardedLevel));
    }

    private void UpdateCoinText()
    {
        if (coinText != null && gameManager != null)
            coinText.text = gameManager.Coins.ToString();
    }

    private void SetupLevelRewardBar()
    {
        if (levelRewardBar == null)
            return;

        levelRewardBar.type = Image.Type.Filled;
        levelRewardBar.fillMethod = Image.FillMethod.Horizontal;
        levelRewardBar.fillOrigin = (int)Image.OriginHorizontal.Left;
        levelRewardBar.color = levelRewardFillColor;

        int lastCompletedLevel = gameManager != null ? gameManager.LastRewardedLevel : 0;
        levelRewardBar.fillAmount = GetRewardBarProgress(lastCompletedLevel);
    }

    private IEnumerator UpdateLevelRewardBarNextFrame(int lastCompletedLevel)
    {
        yield return null;

        if (levelRewardBar == null)
            yield break;

        float from = levelRewardBar.fillAmount;
        float to = GetRewardBarProgress(lastCompletedLevel);
        float elapsed = 0f;

        while (elapsed < rewardBarFillDuration)
        {
            elapsed += Time.deltaTime;
            levelRewardBar.fillAmount = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / rewardBarFillDuration));
            yield return null;
        }

        levelRewardBar.fillAmount = to;
    }

    private float GetRewardBarProgress(int lastCompletedLevel)
    {
        int segmentCount = Mathf.Max(1, totalRewardBarSegments);
        return Mathf.Clamp01((float)Mathf.Max(0, lastCompletedLevel) / segmentCount);
    }

    private int GetLevelNumber()
    {
        if (levelNumberOverride > 0)
            return levelNumberOverride;

        string numberPart = System.Text.RegularExpressions.Regex.Match(SceneManager.GetActiveScene().name, @"\d+").Value;
        if (int.TryParse(numberPart, out int parsedLevelNumber))
            return parsedLevelNumber;

        Debug.LogWarning("GameBoardManager: level number scene name se nahi mila. Level Number Override set kar dein.");
        return 0;
    }

    private IEnumerator PlayWisdomWinAnimation(int fromValue, int toValue)
    {
        if (sunCircle != null)
            sunCircle.localScale = finalSunScale * sunStartScale;

        if (wisdomText != null)
            wisdomText.text = fromValue.ToString();

        float elapsed = 0f;
        while (elapsed < wisdomAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / wisdomAnimationDuration);
            float eased = EaseOutWisdomBack(progress);

            if (sunCircle != null)
                sunCircle.localScale = finalSunScale * Mathf.LerpUnclamped(sunStartScale, 1f, eased);

            if (wisdomText != null)
                wisdomText.text = Mathf.RoundToInt(Mathf.Lerp(fromValue, toValue, progress)).ToString();

            yield return null;
        }

        if (sunCircle != null)
            sunCircle.localScale = finalSunScale;

        if (wisdomText != null)
            wisdomText.text = toValue.ToString();
    }

    private IEnumerator PlayWisdomWinAnimationNextFrame(int fromValue, int toValue)
    {
        yield return null;
        yield return StartCoroutine(PlayWisdomWinAnimation(fromValue, toValue));
    }

    private float EaseOutWisdomBack(float progress)
    {
        float c1 = 1.70158f * Mathf.Max(1.3f, sunOvershootScale);
        float c3 = c1 + 1f;
        float adjusted = progress - 1f;
        return 1f + c3 * adjusted * adjusted * adjusted + c1 * adjusted * adjusted;
    }

    private void ShowGameLoss()
    {
        levelEnded = true;
        SetBoardInputBlocked(true);

        if (gameLossPanel != null)
            gameLossPanel.SetActive(true);

        onGameOver?.Invoke();
    }

    public void SetBoardInputBlocked(bool blocked)
    {
        inputBlocked = blocked;

        BoardTile[] tiles = GetComponentsInChildren<BoardTile>(true);
        foreach (BoardTile tile in tiles)
        {
            Button button = tile.GetComponent<Button>();
            if (button != null)
                button.interactable = !blocked;

            Image image = tile.GetComponent<Image>();
            if (image != null)
                image.raycastTarget = !blocked;
        }
    }

    public bool IsInputBlocked()
    {
        return inputBlocked;
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

                if (playMatchBurst && matchBurstEffect != null)
                    matchBurstEffect.PlayBurst(traySlots[a].rectTransform.position);

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