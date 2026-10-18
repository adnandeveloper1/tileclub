using UnityEngine;
using UnityEngine.UI;

public class SmokeSetup : MonoBehaviour
{
    [Header("Spawn")]
    public int poolSize = 12;
    public float spawnInterval = 0.8f;

    [Header("Rise")]
    public float lifetime = 6f;
    public float risePerSecond = 80f;   // pixels per second

    [Header("Look")]
    public Color smokeColor = new Color(0.85f, 0.85f, 0.88f, 1f); // dark white / halka grey
    public Vector2 puffSize = new Vector2(300f, 600f);
    public float startScale = 0.3f;
    public float endScale = 1.0f;
    public float maxAlpha = 0.45f;

    [Header("Procedural texture")]
    public int variants = 3;
    public int texWidth = 128;
    public int texHeight = 256;
    public float wispiness = 2.2f;

    [Header("Motion")]
    public float swayPixels = 30f;
    public float swaySpeed = 0.8f;
    public float rotateSpeed = 8f;

    class Puff
    {
        public RectTransform rt;
        public Image img;
        public bool active;
        public float age, life, phase, rotDir, baseRot, speedMul;
    }

    Puff[] pool;
    Sprite[] sprites;
    Texture2D[] textures;
    float spawnTimer;

    void Awake()
    {
        // 1) Smoke ki textures script se banayein
        sprites = new Sprite[variants];
        textures = new Texture2D[variants];
        for (int i = 0; i < variants; i++)
            sprites[i] = MakeSmokeSprite(i + 1, out textures[i]);

        // 2) Puffs ka pool
        pool = new Puff[poolSize];

        Vector2 anchor = new Vector2(0.5f, 0.5f);
        var myRect = transform as RectTransform;
        if (myRect != null) anchor = myRect.pivot;

        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject("Puff", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.1f);
            rt.sizeDelta = puffSize;

            var img = go.AddComponent<Image>();
            img.sprite = sprites[i % variants];
            img.raycastTarget = false;
            img.color = new Color(smokeColor.r, smokeColor.g, smokeColor.b, 0f);
            go.SetActive(false);

            pool[i] = new Puff { rt = rt, img = img };
        }
    }

    void OnDestroy()
    {
        if (textures == null) return;
        foreach (var t in textures) if (t) Destroy(t);
        foreach (var s in sprites) if (s) Destroy(s);
    }

    Sprite MakeSmokeSprite(int seed, out Texture2D tex)
    {
        int w = texWidth, h = texHeight;
        tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        var px = new Color32[w * h];
        float ox = seed * 37.1f, oy = seed * 17.3f;

        for (int y = 0; y < h; y++)
        {
            float ny = (float)y / (h - 1);

            float cx = 0.5f + (Mathf.PerlinNoise(ox + ny * 1.5f, 7.7f) - 0.5f) * 0.35f;
            float halfW = Mathf.Lerp(0.08f, 0.40f, ny);
            float vert = Mathf.SmoothStep(0f, 1f, ny / 0.12f)
                       * (1f - Mathf.SmoothStep(0.55f, 1f, ny));

            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / (w - 1);

                float wx = Mathf.PerlinNoise(ox + nx * 2f, oy + ny * 2f) - 0.5f;
                float wy = Mathf.PerlinNoise(ox + 50f + nx * 2f, oy + 50f + ny * 2f) - 0.5f;
                float u = nx + wx * 0.6f;
                float v = ny + wy * 0.4f;

                float n = 0f, amp = 0.5f, f = 3f;
                for (int o = 0; o < 4; o++)
                {
                    n += Mathf.PerlinNoise(ox + u * f, oy + v * f * 0.7f) * amp;
                    amp *= 0.5f;
                    f *= 2f;
                }

                float dx = Mathf.Abs(nx - cx) / halfW;
                float shape = 1f - Mathf.Clamp01(dx);
                shape = shape * shape * (3f - 2f * shape);

                float a = Mathf.Clamp01((n - 0.35f) * wispiness) * shape * vert;
                px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 100f);
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            Spawn();
        }

        foreach (var p in pool)
        {
            if (!p.active) continue;

            p.age += Time.deltaTime;
            float k = p.age / p.life;

            if (k >= 1f)
            {
                p.active = false;
                p.rt.gameObject.SetActive(false);
                continue;
            }

            float sway = Mathf.Sin(p.age * swaySpeed + p.phase) * swayPixels * k;
            p.rt.anchoredPosition = new Vector2(sway, risePerSecond * p.speedMul * p.age);

            float s = Mathf.Lerp(startScale, endScale, Mathf.SmoothStep(0f, 1f, k));
            p.rt.localScale = new Vector3(s, s, 1f);

            p.rt.localRotation = Quaternion.Euler(0, 0, p.baseRot + p.rotDir * rotateSpeed * p.age);

            float a = Mathf.Clamp01(k / 0.15f) * (1f - Mathf.SmoothStep(0.5f, 1f, k));
            p.img.color = new Color(smokeColor.r, smokeColor.g, smokeColor.b, a * maxAlpha);
        }
    }

    void Spawn()
    {
        foreach (var p in pool)
        {
            if (p.active) continue;

            p.active = true;
            p.age = 0f;
            p.life = lifetime * Random.Range(0.85f, 1.15f);
            p.phase = Random.value * 6.28f;
            p.rotDir = Random.value < 0.5f ? -1f : 1f;
            p.baseRot = Random.Range(-10f, 10f);
            p.speedMul = Random.Range(0.85f, 1.15f);

            p.rt.anchoredPosition = Vector2.zero;
            p.rt.localScale = new Vector3(startScale, startScale, 1f);
            p.img.color = new Color(smokeColor.r, smokeColor.g, smokeColor.b, 0f);
            p.rt.gameObject.SetActive(true);
            return;
        }
    }
}