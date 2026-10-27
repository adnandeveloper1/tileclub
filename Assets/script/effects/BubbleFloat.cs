using UnityEngine;
using UnityEngine.UI;

public class BubbleFloat : MonoBehaviour
{
    [Header("Bubble image")]
    public Sprite bubbleSprite;
    public Color tint = Color.white;
    public float maxAlpha = 0.9f;

    [Header("Spawn")]
    public int poolSize = 15;
    public float spawnInterval = 0.5f;
    public Vector2 sizeRange = new Vector2(50f, 110f);   // pixels

    [Header("Motion")]
    public Vector2 lifetimeRange = new Vector2(3f, 6f);
    public Vector2 driftPerSecond = new Vector2(6f, 20f); // x = side, y = upar (pixels/sec)
    public float swayPixels = 12f;
    public float swaySpeed = 1.2f;

    [Header("Blop (nikalte waqt uchhalna)")]
    public float blopTime = 0.35f;     // kitni der mein bara ho
    public float overshoot = 2.2f;     // zyada = zyada uchhalta hai

    [Header("Wobble (pichakna phailna)")]
    public float wobbleAmount = 0.07f;
    public float wobbleSpeed = 5f;

    [Header("Pop (aakhir mein phatna)")]
    public float popTime = 0.18f;
    public float popScale = 1.35f;

    class Bubble
    {
        public RectTransform rt;
        public Image img;
        public bool active;
        public float age, life, phase, swayMul, wobbleMul, size;
        public Vector3 origin;
    }

    Bubble[] pool;
    RectTransform area;
    float spawnTimer;

    void Awake()
    {
        area = transform as RectTransform;

        pool = new Bubble[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject("Bubble", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = go.AddComponent<Image>();
            img.sprite = bubbleSprite;
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.color = new Color(tint.r, tint.g, tint.b, 0f);
            go.SetActive(false);

            pool[i] = new Bubble { rt = rt, img = img };
        }
    }

    // Ease-out-back: tezi se bara ho, thora zyada phoole, phir wapas theek size par
    float EaseOutBack(float x)
    {
        float c1 = overshoot;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            Spawn();
        }

        foreach (var b in pool)
        {
            if (!b.active) continue;

            b.age += Time.deltaTime;

            if (b.age >= b.life)
            {
                b.active = false;
                b.rt.gameObject.SetActive(false);
                continue;
            }

            // Position: upar jana + lehrana
            float sway = Mathf.Sin(b.age * swaySpeed + b.phase) * swayPixels * b.swayMul;
            b.rt.localPosition = b.origin + new Vector3(
                driftPerSecond.x * b.age + sway,
                driftPerSecond.y * b.age, 0f);

            // 1) Blop: nikalte hi uchhal kar bara
            float s = 1f;
            float alpha = 1f;
            if (b.age < blopTime)
            {
                float x = b.age / blopTime;
                s = EaseOutBack(x);
                alpha = Mathf.Clamp01(x * 3f);
            }

            // 2) Wobble: chaura hota hai to lamba kam, lamba hota hai to chaura kam
            float w = Mathf.Sin(b.age * wobbleSpeed * b.wobbleMul + b.phase) * wobbleAmount;
            float sx = s * (1f + w);
            float sy = s * (1f - w);

            // 3) Pop: aakhir mein tezi se phoolna aur ghayab hona
            float timeLeft = b.life - b.age;
            if (timeLeft < popTime)
            {
                float p = 1f - timeLeft / popTime;      // 0..1
                float grow = Mathf.Lerp(1f, popScale, p);
                sx *= grow;
                sy *= grow;
                alpha = 1f - p;
            }

            b.rt.localScale = new Vector3(sx, sy, 1f);
            b.img.color = new Color(tint.r, tint.g, tint.b, alpha * maxAlpha);
        }
    }

    void Spawn()
    {
        foreach (var b in pool)
        {
            if (b.active) continue;

            Rect r = area.rect;
            b.active = true;
            b.age = 0f;
            b.life = Random.Range(lifetimeRange.x, lifetimeRange.y);
            b.phase = Random.value * 6.28f;
            b.swayMul = Random.Range(0.6f, 1.3f);
            b.wobbleMul = Random.Range(0.8f, 1.3f);
            b.size = Random.Range(sizeRange.x, sizeRange.y);
            b.origin = new Vector3(
                Random.Range(r.xMin, r.xMax),
                Random.Range(r.yMin, r.yMax), 0f);

            b.rt.sizeDelta = new Vector2(b.size, b.size);
            b.rt.localPosition = b.origin;
            b.rt.localScale = Vector3.zero;
            b.img.sprite = bubbleSprite;
            b.img.color = new Color(tint.r, tint.g, tint.b, 0f);
            b.rt.gameObject.SetActive(true);
            return;
        }
    }
}