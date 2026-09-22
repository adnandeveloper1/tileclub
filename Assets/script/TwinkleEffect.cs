using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes a sparkle/star image "twinkle" — blinks on (fade + scale up), holds
/// briefly, then fades back off, then waits before doing it again. Like a
/// little light turning on and off.
///
/// SETUP: Put the sparkle Image as a child positioned on the corner/edge of
/// your gift icon (wherever you want the twinkle to appear), attach this script.
/// </summary>
[RequireComponent(typeof(Image))]
public class TwinkleEffect : MonoBehaviour
{
    [Header("Blink timing")]
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private float holdDuration = 0.1f;
    [SerializeField] private float fadeOutDuration = 0.25f;
    [SerializeField] private float delayBetweenTwinkles = 1.5f;

    [Header("Scale pop (optional — set both to 1 to disable)")]
    [SerializeField] private float startScale = 0.3f;
    [SerializeField] private float peakScale = 1f;

    [Header("Randomize so multiple sparkles don't blink in sync")]
    [SerializeField] private bool randomizeStartDelay = true;
    [SerializeField] private float maxRandomStartDelay = 1.5f;

    private Image image;
    private RectTransform rect;

    private void Awake()
    {
        image = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
        SetVisible(0f, startScale);
    }

    private void OnEnable()
    {
        StartCoroutine(TwinkleLoop());
    }

    private IEnumerator TwinkleLoop()
    {
        if (randomizeStartDelay)
            yield return new WaitForSeconds(Random.Range(0f, maxRandomStartDelay));

        while (true)
        {
            yield return StartCoroutine(FadeTo(1f, peakScale, fadeInDuration));
            yield return new WaitForSeconds(holdDuration);
            yield return StartCoroutine(FadeTo(0f, startScale, fadeOutDuration));
            yield return new WaitForSeconds(delayBetweenTwinkles);
        }
    }

    private IEnumerator FadeTo(float targetAlpha, float targetScale, float duration)
    {
        float startAlpha = image.color.a;
        float fromScale = rect.localScale.x;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;

            SetVisible(Mathf.Lerp(startAlpha, targetAlpha, p), Mathf.Lerp(fromScale, targetScale, p));
            yield return null;
        }

        SetVisible(targetAlpha, targetScale);
    }

    private void SetVisible(float alpha, float scale)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
        rect.localScale = Vector3.one * scale;
    }
}