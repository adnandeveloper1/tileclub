using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generates and animates a falling autumn-leaf effect from a UI object.
/// Assign a leaf sprite in the Inspector, then call PlayBurst().
/// </summary>
public class SprinkleBurstEffect : MonoBehaviour
{
    [Header("Autumn leaves")]
    [Tooltip("Assign the leaf image Sprite here.")]
    [SerializeField] private Sprite leafSprite;
    [SerializeField] private int leafCount = 35;
    [SerializeField] private Vector2 leafSize = new Vector2(24f, 24f);
    [Tooltip("Horizontal spread of the falling leaves in local UI pixels.")]
    [SerializeField] private float fallWidth = 420f;
    [Tooltip("Height above the effect object where leaves begin.")]
    [SerializeField] private float startHeight = 260f;
    [Tooltip("Height below the effect object where leaves finish.")]
    [SerializeField] private float endHeight = -260f;
    [SerializeField] private float fallDuration = 1.35f;
    [SerializeField] private float sideSway = 55f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float startDelaySpread = 0.35f;

    private readonly List<Image> leafImages = new List<Image>();

    private void Awake()
    {
        GenerateLeaves();
    }

    public void PlayBurst()
    {
        if (leafSprite == null)
            return;

        for (int i = 0; i < leafImages.Count; i++)
        {
            if (leafImages[i] == null)
                continue;

            StartCoroutine(AnimateLeaf(leafImages[i], i));
        }
    }

    private void GenerateLeaves()
    {
        for (int i = 0; i < leafCount; i++)
        {
            GameObject leafObject = new GameObject("Leaf_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            leafObject.transform.SetParent(transform, false);

            RectTransform rect = leafObject.GetComponent<RectTransform>();
            rect.sizeDelta = leafSize;

            Image image = leafObject.GetComponent<Image>();
            image.sprite = leafSprite;
            image.raycastTarget = false;
            image.enabled = false;
            leafImages.Add(image);
        }
    }

    private IEnumerator AnimateLeaf(Image leaf, int index)
    {
        if (startDelaySpread > 0f)
            yield return new WaitForSeconds(Random.Range(0f, startDelaySpread));

        RectTransform rect = leaf.rectTransform;
        float startX = Random.Range(-fallWidth * 0.5f, fallWidth * 0.5f);
        float swayAmount = Random.Range(sideSway * 0.6f, sideSway * 1.4f);
        float swaySpeed = Random.Range(1.2f, 2.2f);
        float leafRotationSpeed = Random.Range(-rotationSpeed, rotationSpeed);
        float swayPhase = Random.Range(0f, Mathf.PI * 2f);
        rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        rect.anchoredPosition = new Vector2(startX, startHeight);
        leaf.enabled = true;
        float elapsed = 0f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fallDuration);
            float smoothProgress = progress * progress * (3f - 2f * progress);
            float y = Mathf.Lerp(startHeight, endHeight, smoothProgress);
            float x = startX + Mathf.Sin((progress * swaySpeed * Mathf.PI * 2f) + swayPhase) * swayAmount;
            rect.anchoredPosition = new Vector2(x, y);
            rect.Rotate(0f, 0f, leafRotationSpeed * Time.deltaTime);
            SetAlpha(leaf, Mathf.Clamp01(1f - Mathf.Max(0f, progress - 0.75f) / 0.25f));
            yield return null;
        }

        leaf.enabled = false;
    }

    private void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
