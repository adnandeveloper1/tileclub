using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fills a bar between two marker points using Unity's built-in
/// Image "Filled" type — the RIGHT way to combine your start/end-point idea
/// with a proper fill (handles rounded-corner sprites correctly, no manual
/// pixel/width hacking needed).
///
/// SETUP:
///  1. Make an empty GameObject "BarStartPoint" at the LEFT edge of your bar track.
///  2. Make an empty GameObject "BarEndPoint" at the RIGHT edge of your bar track.
///  3. Make your colored "FillBar" Image (can be your rounded-rect sprite).
///     Set its Image component: Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left.
///     Set its RectTransform Pivot to X=0, Y=0.5 (left-center).
///  4. Drag all three into this script's fields.
///
/// How it works: ONCE at Start, this script sets the fill bar's position
/// and width to exactly span from startPoint to endPoint (so it never needs
/// touching again). After that, SetProgress()/AnimateProgress() only ever
/// change fillAmount (0–1) — which is what actually reveals/hides the
/// colored sprite. This is why it's more reliable than resizing width by hand.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class FillBarByPoints : MonoBehaviour
{
    [SerializeField] private RectTransform startPoint;
    [SerializeField] private RectTransform endPoint;
    [SerializeField] private Image fillImage;
    [SerializeField] private float fillHeight = 14f;

    private void Start()
    {
        SetupBarBounds();
    }

    /// <summary>Positions and sizes the fill bar ONCE to span start→end. Safe to call again if points move (e.g. on screen resize).</summary>
    public void SetupBarBounds()
    {
        if (fillImage == null || startPoint == null || endPoint == null) return;

        RectTransform fillRect = fillImage.rectTransform;

        // Make sure it's actually set up as a Filled image — this is the
        // whole point: Filled type + fixed-size container + fillAmount.
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

        fillRect.pivot = new Vector2(0f, 0.5f);

        float totalDistance = Vector3.Distance(startPoint.position, endPoint.position);
        fillRect.position = startPoint.position;
        fillRect.sizeDelta = new Vector2(totalDistance, fillHeight);
    }

    /// <summary>Instantly sets the fill to a 0–1 progress value.</summary>
    public void SetProgress(float progress01)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = Mathf.Clamp01(progress01);
    }

    /// <summary>Animates smoothly from one progress value to another over `duration` seconds.</summary>
    public IEnumerator AnimateProgress(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetProgress(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;
            SetProgress(Mathf.Lerp(from, to, p));
            yield return null;
        }
        SetProgress(to);
    }
}