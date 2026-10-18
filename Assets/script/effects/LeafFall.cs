using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes a leaf/petal image fall from the top of the screen to the bottom,
/// swaying gently side to side and slowly rotating — then loops back to the
/// top once it falls off-screen. Same idea as BirdFlyer/CloudDrift, just
/// moving vertically instead of horizontally.
///
/// Put this on multiple leaf Images under your tree, each with a different
/// "Start Delay" / "Fall Speed" / "Sway Speed" so they don't all fall in
/// sync — that's what sells the "randomly drifting" look.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class LeafFall : MonoBehaviour
{
    [Header("Fall")]
    [SerializeField] private float fallSpeed = 40f;      // pixels per second, keep slow
    [SerializeField] private float startDelay = 0f;       // seconds to wait before the first fall (stagger multiple leaves)

    [Header("Sway (side to side drift while falling)")]
    [SerializeField] private float swayAmplitude = 25f;
    [SerializeField] private float swaySpeed = 1.5f;

    [Header("Spin")]
    [SerializeField] private float rotationSpeed = 40f;   // degrees per second
    [SerializeField] private bool randomizeRotationDirection = true;

    [Header("Spawn area")]
    [Tooltip("Leave this at 0 — it's set automatically from wherever you place the leaf in the Editor.")]
    [SerializeField] private float startX = 0f;
    [SerializeField] private float startXVariance = 30f;

    private RectTransform rect;
    private RectTransform parentRect;
    private float elapsed;
    private float spinDir;
    private bool waiting;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        parentRect = transform.parent as RectTransform;
        spinDir = randomizeRotationDirection && Random.value > 0.5f ? -1f : 1f;

        // Use wherever you manually placed this leaf in the Editor as its
        // "home" X position, instead of forcing it to a fixed spot.
        startX = rect.anchoredPosition.x;

        // Start falling from exactly where you placed it in the Editor —
        // only future loops (after it falls off-screen) snap back to the top.
        waiting = startDelay > 0f;
        elapsed = 0f;
    }

    private void Update()
    {
        if (waiting)
        {
            elapsed += Time.deltaTime;
            if (elapsed < startDelay) return;
            waiting = false;
        }

        Vector2 pos = rect.anchoredPosition;
        pos.y -= fallSpeed * Time.deltaTime;
        pos.x = startX + Mathf.Sin(Time.time * swaySpeed) * swayAmplitude;
        rect.anchoredPosition = pos;

        rect.Rotate(0f, 0f, rotationSpeed * spinDir * Time.deltaTime);

        float bottomLimit = -(parentRect.rect.height / 2f) - 50f;
        if (pos.y < bottomLimit)
            ResetToTop();
    }

    private void ResetToTop()
    {
        float topY = parentRect.rect.height / 2f + 50f;
        float randomX = startX + Random.Range(-startXVariance, startXVariance);
        rect.anchoredPosition = new Vector2(randomX, topY);
    }
}