using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class CloudDrift : MonoBehaviour
{
    [Header("Drift")]
    [SerializeField] private float driftSpeed = 12f;   // pixels per second, keep this slow
    [SerializeField] private bool driftRightToLeft = false;

    [Header("Formation (stops multiple clouds overlapping)")]
    [Tooltip("Extra distance BEHIND the spawn edge. Give each cloud a different value (e.g. 0, 200, 450) so they stay spaced apart instead of stacking.")]
    [SerializeField] private float startOffset = 0f;

    [Header("Optional gentle vertical bob")]
    [SerializeField] private float bobAmplitude = 0f;
    [SerializeField] private float bobSpeed = 0.5f;

    private RectTransform rect;
    private RectTransform canvasRect;
    private float startY;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasRect = GetComponentInParent<Canvas>().transform as RectTransform;
        startY = rect.anchoredPosition.y;

        ResetToStartEdge();
    }

    private void Update()
    {
        float direction = driftRightToLeft ? -1f : 1f;
        Vector2 pos = rect.anchoredPosition;
        pos.x += direction * driftSpeed * Time.deltaTime;

        if (bobAmplitude > 0f)
            pos.y = startY + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;

        rect.anchoredPosition = pos;

        float halfWidth = canvasRect.rect.width / 2f + 150f;
        if (!driftRightToLeft && pos.x > halfWidth)
            ResetToStartEdge();
        else if (driftRightToLeft && pos.x < -halfWidth)
            ResetToStartEdge();
    }

    private void ResetToStartEdge()
    {
        float halfWidth = canvasRect.rect.width / 2f + 150f;
        float startX = driftRightToLeft ? halfWidth + startOffset : -halfWidth - startOffset;
        rect.anchoredPosition = new Vector2(startX, startY);
    }
}