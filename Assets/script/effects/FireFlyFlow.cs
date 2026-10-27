using UnityEngine;
using UnityEngine.UI;

public class FireflyFlow : MonoBehaviour
{
    [Header("Fireflies")]
    public int count = 6;
    public Color glowColor = new Color(1f, 0.85f, 0.25f, 1f); // peela
    public float size = 40f;            // glow ka size (pixels)

    [Header("Movement")]
    public float moveSpeed = 0.15f;     // ghoomne ki speed (chhota = ahista)
    public float bobAmount = 6f;        // chhota sa upar neeche phadakna (pixels)

    [Header("Blink")]
    public float blinkSpeed = 1.6f;
    public float minAlpha = 0.15f;
    public float maxAlpha = 1f;

    class Fly
    {
        public RectTransform rt;
        public Image img;
        public float seedX, seedY, phase, speedMul, sizeMul;
    }

    Fly[] flies;
    Sprite glowSprite;
    Texture2D glowTex;
    RectTransform area;

    void Awake()
    {
        area = transform as RectTransform;
        glowSprite = MakeGlowSprite();

        flies = new Fly[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Firefly", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = go.AddComponent<Image>();
            img.sprite = glowSprite;
            img.raycastTarget = false;
            img.color = glowColor;

            float sizeMul = Random.Range(0.7f, 1.2f);
            rt.sizeDelta = new Vector2(size * sizeMul, size * sizeMul);

            flies[i] = new Fly
            {
                rt = rt,
                img = img,
                seedX = Random.value * 100f,
                seedY = Random.value * 100f,
                phase = Random.value * 6.28f,
                speedMul = Random.Range(0.7f, 1.3f),
                sizeMul = sizeMul
            };
        }
    }

    void OnDestroy()
    {
        if (glowTex) Destroy(glowTex);
        if (glowSprite) Destroy(glowSprite);
    }

    Sprite MakeGlowSprite()
    {
        int s = 64;
        glowTex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        glowTex.wrapMode = TextureWrapMode.Clamp;
        glowTex.filterMode = FilterMode.Bilinear;

        var px = new Color32[s * s];
        float c = (s - 1) * 0.5f;

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;  // 0 center, 1 kinara
                d = Mathf.Clamp01(d);

                float core = Mathf.Exp(-d * d * 30f);                    // chhota chamakta point
                float halo = Mathf.Pow(1f - d, 3f) * 0.45f;              // naram roshni
                float a = Mathf.Clamp01(core + halo);

                px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        glowTex.SetPixels32(px);
        glowTex.Apply();
        return Sprite.Create(glowTex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    void Update()
    {
        float halfW = area != null ? area.rect.width * 0.5f : 200f;
        float halfH = area != null ? area.rect.height * 0.5f : 150f;
        float t = Time.time;

        foreach (var f in flies)
        {
            // Perlin noise se rasta, 0..1 ko -1..1 mein badal kar area par phailaya
            float nx = (Mathf.PerlinNoise(f.seedX, t * moveSpeed * f.speedMul) - 0.5f) * 2f * 1.5f;
            float ny = (Mathf.PerlinNoise(f.seedY, t * moveSpeed * f.speedMul) - 0.5f) * 2f * 1.5f;

            float x = Mathf.Clamp(nx, -1f, 1f) * halfW;
            float y = Mathf.Clamp(ny, -1f, 1f) * halfH;

            // Chhota sa phadakna (jugnu seedha nahi urta)
            y += Mathf.Sin(t * 3f * f.speedMul + f.phase) * bobAmount;
            x += Mathf.Cos(t * 2.1f * f.speedMul + f.phase) * bobAmount * 0.6f;

            f.rt.anchoredPosition = new Vector2(x, y);

            // Chamak: roshan, bujhe, phir roshan
            float s = (Mathf.Sin(t * blinkSpeed * f.speedMul + f.phase) + 1f) * 0.5f;
            s = Mathf.SmoothStep(0f, 1f, s);
            float a = Mathf.Lerp(minAlpha, maxAlpha, s);

            f.img.color = new Color(glowColor.r, glowColor.g, glowColor.b, a);

            // Roshan hote waqt thora bara
            float sc = Mathf.Lerp(0.7f, 1.15f, s);
            f.rt.localScale = new Vector3(sc, sc, 1f);
        }
    }
}