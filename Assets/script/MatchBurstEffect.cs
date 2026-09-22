using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns a quick radial burst of small "shard" images (plus a glowing
/// spark) flying outward from a point and fading away — used right when 3
/// tiles match and clear from the tray.
///
/// Put this on a dedicated 'EffectsLayer' GameObject that's the LAST child
/// under your Canvas (so bursts always render on top of tiles/tray).
/// </summary>
public class MatchBurstEffect : MonoBehaviour
{
    [Header("Shard prefabs (drag a few different shard sprites for variety)")]
    [SerializeField] private List<Image> shardPrefabs;
    [SerializeField] private Image sparkPrefab;

    [Header("Burst shape")]
    [SerializeField] private int shardCount = 35;
    [SerializeField] private float minDistance = 150f;
    [SerializeField] private float maxDistance = 400f;
    [SerializeField] private float duration = 0.7f;
    [SerializeField] private float spinSpeed = 260f;

    /// <summary>Call this the moment a match clears, passing the world position where it happened.</summary>
    public void PlayBurst(Vector3 worldPosition)
    {
        if (shardPrefabs == null || shardPrefabs.Count == 0) return;

        for (int i = 0; i < shardCount; i++)
        {
            Image prefab = shardPrefabs[Random.Range(0, shardPrefabs.Count)];
            Image shard = Instantiate(prefab, transform);
            StartCoroutine(AnimatePiece(shard, worldPosition));
        }

        if (sparkPrefab != null)
        {
            Image spark = Instantiate(sparkPrefab, transform);
            StartCoroutine(AnimatePiece(spark, worldPosition, isSpark: true));
        }
    }

    private IEnumerator AnimatePiece(Image piece, Vector3 origin, bool isSpark = false)
    {
        piece.rectTransform.position = origin;
        Vector3 startScale = Vector3.one * Random.Range(0.8f, 1.3f);
        piece.rectTransform.localScale = startScale;
        piece.rectTransform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = isSpark ? minDistance * 0.5f : Random.Range(minDistance, maxDistance);
        Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        Vector3 target = origin + direction * distance;
        float fallDistance = Random.Range(30f, 90f); // gentle gravity drift as it flies out, like confetti

        float spin = Random.Range(-1f, 1f) * spinSpeed;
        Color startColor = piece.color;
        startColor.a = 1f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;

            // Ease-out: fly outward fast at first, then slow down (feels like a real burst).
            float eased = 1f - (1f - p) * (1f - p);
            float arc = Mathf.Sin(p * Mathf.PI) * 12f;
            float gravity = fallDistance * p * p;
            piece.rectTransform.position = Vector3.Lerp(origin, target, eased) + Vector3.up * (arc - gravity);
            piece.rectTransform.Rotate(0f, 0f, spin * Time.deltaTime);

            // Stay full size/opacity while flying out, only shrink+fade in the last 40%.
            float fadeP = Mathf.Clamp01((p - 0.6f) / 0.4f);
            piece.rectTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, fadeP);

            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, fadeP);
            piece.color = c;

            yield return null;
        }

        Destroy(piece.gameObject);
    }
}