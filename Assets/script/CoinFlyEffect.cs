using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Flies several coin UI images from this object toward a target coin UI element.
/// </summary>
public class CoinFlyEffect : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the coin sprite/image used by each flying coin.")]
    [SerializeField] private Sprite coinSprite;
    [Tooltip("Assign the coin bar or coin TMP RectTransform as the destination.")]
    [SerializeField] private RectTransform target;

    [Header("Animation")]
    [SerializeField] private int coinCount = 8;
    [SerializeField] private Vector2 coinSize = new Vector2(30f, 30f);
    [SerializeField] private float startSpread = 70f;
    [SerializeField] private float arcHeight = 90f;
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float stagger = 0.06f;

    private readonly List<Image> coinImages = new List<Image>();

    private void Awake()
    {
        GenerateCoins();
    }

    public void PlayBurst()
    {
        if (coinSprite == null)
        {
            Debug.LogWarning($"{nameof(CoinFlyEffect)} on {name} cannot play: Coin Sprite is not assigned.", this);
            return;
        }

        if (target == null)
        {
            Debug.LogWarning($"{nameof(CoinFlyEffect)} on {name} cannot play: Target is not assigned.", this);
            return;
        }

        for (int i = 0; i < coinImages.Count; i++)
        {
            if (coinImages[i] != null)
                StartCoroutine(AnimateCoin(coinImages[i], i));
        }
    }

    private void GenerateCoins()
    {
        for (int i = 0; i < coinCount; i++)
        {
            GameObject coinObject = new GameObject("FlyingCoin_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            coinObject.transform.SetParent(transform, false);

            RectTransform rect = coinObject.GetComponent<RectTransform>();
            rect.sizeDelta = coinSize;

            Image image = coinObject.GetComponent<Image>();
            image.sprite = coinSprite;
            image.raycastTarget = false;
            image.enabled = false;
            coinImages.Add(image);
        }
    }

    private IEnumerator AnimateCoin(Image coin, int index)
    {
        if (stagger > 0f && index > 0)
            yield return new WaitForSeconds(index * stagger);

        RectTransform rect = coin.rectTransform;
        Vector3 start = transform.position + new Vector3(
            Random.Range(-startSpread, startSpread),
            Random.Range(-startSpread, startSpread),
            0f);
        Vector3 end = target.position;
        Vector3 control = Vector3.Lerp(start, end, 0.5f) + Vector3.up * arcHeight;

        coin.enabled = true;
        rect.position = start;
        rect.localScale = Vector3.one * Random.Range(0.8f, 1.15f);
        rect.rotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = progress * progress * (3f - 2f * progress);
            Vector3 first = Vector3.Lerp(start, control, eased);
            Vector3 second = Vector3.Lerp(control, end, eased);
            rect.position = Vector3.Lerp(first, second, eased);
            rect.Rotate(0f, 0f, 360f * Time.deltaTime);
            rect.localScale = Vector3.Lerp(rect.localScale, Vector3.one, Time.deltaTime * 8f);
            SetAlpha(coin, 1f - Mathf.Clamp01((progress - 0.8f) / 0.2f));
            yield return null;
        }

        rect.position = end;
        coin.enabled = false;
    }

    private void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
