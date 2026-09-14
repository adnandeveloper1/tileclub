using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes a small bird silhouette fly across the sky, flapping its wings
/// (by swapping between 2 sprites) and gently bobbing up and down.
/// Loops back to the opposite edge once it flies off-screen.
///
/// Setup:
///  1. Make a UI Image GameObject, small (e.g. 24x16), place it in the sky area.
///  2. Give it 2 simple silhouette sprites: wings-up and wings-down.
///  3. Attach this script, drag both sprites into "Flap Frames".
/// </summary>
[RequireComponent(typeof(RectTransform), typeof(Image))]
public class BirdFlyer : MonoBehaviour
{
    [Header("Flap animation (2 frames is enough at this size)")]
    [SerializeField] private Sprite[] flapFrames;
    [SerializeField] private float flapsPerSecond = 6f;

    [Header("Flight path")]
    [SerializeField] private float flightSpeed = 60f;       // pixels per second
    [SerializeField] private float verticalBobAmplitude = 8f;
    [SerializeField] private float verticalBobSpeed = 2f;
    [SerializeField] private bool flyRightToLeft = false;

    [Header("Formation (use this to stop multiple birds overlapping)")]
    [Tooltip("Extra distance BEHIND the spawn edge. Give each bird in a group a different value (e.g. 0, 150, 300) so they spawn staggered and stay spaced apart throughout the flight instead of stacking on top of each other.")]
    [SerializeField] private float startOffset = 0f;

    private RectTransform rect;
    private RectTransform canvasRect;
    private Image image;
    private float flapTimer;
    private int frameIndex;
    private float startY;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        canvasRect = GetComponentInParent<Canvas>().transform as RectTransform;
        startY = rect.anchoredPosition.y;

        ResetToStartEdge();
    }

    private void Update()
    {
        AnimateFlap();
        Fly();
    }

    private void AnimateFlap()
    {
        if (flapFrames == null || flapFrames.Length == 0) return;

        flapTimer += Time.deltaTime;
        if (flapTimer >= 1f / flapsPerSecond)
        {
            flapTimer = 0f;
            frameIndex = (frameIndex + 1) % flapFrames.Length;
            image.sprite = flapFrames[frameIndex];
        }
    }

    private void Fly()
    {
        float direction = flyRightToLeft ? -1f : 1f;
        Vector2 pos = rect.anchoredPosition;
        pos.x += direction * flightSpeed * Time.deltaTime;
        pos.y = startY + Mathf.Sin(Time.time * verticalBobSpeed) * verticalBobAmplitude;
        rect.anchoredPosition = pos;

        // Face the direction of travel.
        Vector3 scale = rect.localScale;
        scale.x = flyRightToLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        rect.localScale = scale;

        float halfWidth = canvasRect.rect.width / 2f + 100f;
        if (!flyRightToLeft && pos.x > halfWidth)
            ResetToStartEdge();
        else if (flyRightToLeft && pos.x < -halfWidth)
            ResetToStartEdge();
    }

    private void ResetToStartEdge()
    {
        float halfWidth = canvasRect.rect.width / 2f + 100f;
        float startX = flyRightToLeft ? halfWidth + startOffset : -halfWidth - startOffset;
        rect.anchoredPosition = new Vector2(startX, startY);
    }
}