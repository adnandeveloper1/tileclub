using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the "HOW TO PLAY" demo animation: a hand cursor taps mini board
/// tiles in a fixed order, each tapped tile "flies" into the next empty
/// tray slot, and whenever 3 matching tiles land in the tray they clear
/// automatically. Loops forever while this GameObject is active.
///
/// Attach this to the 'Tutorial popup' root GameObject (so it starts when
/// the popup is shown, and stops when you SetActive(false) it via OK).
///
/// SETUP NOTES:
///  - Each entry in `traySlots` needs an Image component that starts EMPTY
///    (Source Image = None) and INVISIBLE (Color Alpha = 0). This script
///    fills them in and fades them as tiles arrive.
///  - Arrange your MiniBoardGrid icon sprites in repeating groups of 3
///    (e.g. A,B,C,A,B,C,A,B,C,A,B,C for 12 icons / 4 types) so a match is
///    always found before the 7-slot tray overflows.
/// </summary>
public class TutorialDemoController : MonoBehaviour
{
    [Header("Board (mini tiles, tapped in this order)")]
    [SerializeField] private Image[] boardIcons;

    [Header("Tray (7 slot Images — start with Sprite = None, Alpha = 0)")]
    [SerializeField] private Image[] traySlots;

    [Header("Hand cursor")]
    [SerializeField] private RectTransform handIcon;
    [SerializeField] private Image handImage;

    [Header("Timing")]
    [SerializeField] private float handMoveDuration = 0.4f;
    [SerializeField] private float tapPunchDuration = 0.1f;
    [SerializeField] private float tapPauseDuration = 0.15f;
    [SerializeField] private float flyDuration = 0.35f;
    [SerializeField] private float delayBetweenTaps = 0.5f;
    [SerializeField] private float matchClearDelay = 0.3f;
    [SerializeField] private float loopResetDelay = 1.2f;

    private Vector3[] slotHomePositions;
    private Coroutine demoRoutine;

    private void Awake()
    {
        slotHomePositions = new Vector3[traySlots.Length];
        for (int i = 0; i < traySlots.Length; i++)
            slotHomePositions[i] = traySlots[i].rectTransform.position;
    }

    private void OnEnable()
    {
        ResetEverything();
        demoRoutine = StartCoroutine(RunDemoLoop());
    }

    private void OnDisable()
    {
        if (demoRoutine != null)
            StopCoroutine(demoRoutine);
    }

    private IEnumerator RunDemoLoop()
    {
        SetHandVisible(true);

        while (true)
        {
            foreach (Image boardIcon in boardIcons)
            {
                if (boardIcon == null || !boardIcon.gameObject.activeSelf)
                    continue;

                yield return StartCoroutine(MoveHandTo(boardIcon.rectTransform.position));
                yield return StartCoroutine(PunchScale(handIcon));
                yield return new WaitForSeconds(tapPauseDuration);

                yield return StartCoroutine(SendToTray(boardIcon));

                yield return new WaitForSeconds(delayBetweenTaps);
            }

            // Finished one full pass through the board — pause, reset, loop again.
            yield return new WaitForSeconds(loopResetDelay);
            ResetEverything();
            yield return new WaitForSeconds(0.4f);
        }
    }

    private int FindNextEmptySlot()
    {
        for (int i = 0; i < traySlots.Length; i++)
            if (traySlots[i].sprite == null)
                return i;
        return -1;
    }

    private IEnumerator SendToTray(Image boardIcon)
    {
        int slotIndex = FindNextEmptySlot();
        if (slotIndex == -1)
            yield break; // Tray full — shouldn't happen if board pattern is arranged in triplets.

        Image slot = traySlots[slotIndex];
        slot.sprite = boardIcon.sprite;
        SetAlpha(slot, 1f);
        slot.rectTransform.position = boardIcon.rectTransform.position;

        // "Pick up" the board tile.
        boardIcon.gameObject.SetActive(false);

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
                yield return StartCoroutine(ArrangeAndClearMatches(kvp.Value));
                yield break;
            }
        }
    }
    /// <summary>
/// Before clearing a found match, physically swaps any "odd" tile sitting
/// between the matching tiles with the scattered matching tile, so all 3
/// matching tiles visually land in adjacent slots before they vanish.
/// </summary>
private IEnumerator ArrangeAndClearMatches(List<int> groupIndices)
{
    List<int> group = groupIndices.GetRange(0, 3);
    group.Sort();

    int basePos = group[0];
    int[] targetPositions = { basePos, basePos + 1, basePos + 2 };

    for (int t = 0; t < targetPositions.Length; t++)
    {
        int target = targetPositions[t];
        if (group.Contains(target)) continue; // already sitting correctly

        // find a matching tile that's still outside the target range
        int sourceIdx = -1;
        foreach (int g in group)
        {
            bool alreadyInTarget = System.Array.IndexOf(targetPositions, g) >= 0;
            if (!alreadyInTarget) { sourceIdx = g; break; }
        }
        if (sourceIdx == -1) continue;

        yield return StartCoroutine(SwapTraySlots(target, sourceIdx));

        group.Remove(sourceIdx);
        group.Add(target);
    }

    yield return new WaitForSeconds(matchClearDelay);
    yield return StartCoroutine(ShrinkAndClear(new List<int>(targetPositions)));
    CompactTraySlots();
}

/// <summary>Animates two tray slots swapping their sprite/position with each other.</summary>
private IEnumerator SwapTraySlots(int a, int b)
{
    Image slotA = traySlots[a];
    Image slotB = traySlots[b];

    Sprite spriteA = slotA.sprite;
    Sprite spriteB = slotB.sprite;
    float alphaA = slotA.color.a;
    float alphaB = slotB.color.a;

    Vector3 startA = slotA.rectTransform.position;
    Vector3 startB = slotB.rectTransform.position;
    Vector3 endA = slotHomePositions[b];
    Vector3 endB = slotHomePositions[a];

    float dur = flyDuration * 0.6f;
    float t = 0f;
    while (t < dur)
    {
        t += Time.deltaTime;
        float lerp = t / dur;
        slotA.rectTransform.position = Vector3.Lerp(startA, endA, lerp);
        slotB.rectTransform.position = Vector3.Lerp(startB, endB, lerp);
        yield return null;
    }

    slotA.sprite = spriteB;
    SetAlpha(slotA, alphaB);
    slotA.rectTransform.position = slotHomePositions[a];

    slotB.sprite = spriteA;
    SetAlpha(slotB, alphaA);
    slotB.rectTransform.position = slotHomePositions[b];
}

    /// <summary>Animates the given tray slot indices shrinking to zero scale, then clears their sprite/alpha/scale.</summary>
    private IEnumerator ShrinkAndClear(List<int> slotIndices)
    {
        float dur = 0.2f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 0f, elapsed / dur);
            foreach (int idx in slotIndices)
                traySlots[idx].rectTransform.localScale = Vector3.one * scale;
            yield return null;
        }

        foreach (int idx in slotIndices)
        {
            traySlots[idx].sprite = null;
            SetAlpha(traySlots[idx], 0f);
            traySlots[idx].rectTransform.localScale = Vector3.one; // reset for reuse
        }
    }

    /// <summary>Shifts remaining tray tiles left to fill any gap left by a clear, so the tray always looks packed from slot 0 onward.</summary>
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

    private IEnumerator MoveHandTo(Vector3 worldPos)
    {
        Vector3 start = handIcon.position;
        float t = 0f;
        while (t < handMoveDuration)
        {
            t += Time.deltaTime;
            handIcon.position = Vector3.Lerp(start, worldPos, t / handMoveDuration);
            yield return null;
        }
        handIcon.position = worldPos;
    }

    private IEnumerator PunchScale(RectTransform target)
    {
        Vector3 original = Vector3.one;
        Vector3 small = original * 0.8f;

        float elapsed = 0f;
        while (elapsed < tapPunchDuration)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.Lerp(original, small, elapsed / tapPunchDuration);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < tapPunchDuration)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.Lerp(small, original, elapsed / tapPunchDuration);
            yield return null;
        }
        target.localScale = original;
    }

    private void SetHandVisible(bool visible)
    {
        if (handImage == null) return;
        Color c = handImage.color;
        c.a = visible ? 1f : 0f;
        handImage.color = c;
    }

    private void SetAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    private void ResetEverything()
    {
        foreach (Image icon in boardIcons)
            if (icon != null)
                icon.gameObject.SetActive(true);

        for (int i = 0; i < traySlots.Length; i++)
        {
            traySlots[i].sprite = null;
            SetAlpha(traySlots[i], 0f);
            traySlots[i].rectTransform.position = slotHomePositions[i];
        }
    }
}