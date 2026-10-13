using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TouchEffect: MonoBehaviour
{
    private static TouchEffect instance;

    [Header("Look")]
    //[SerializeField] private Color color = new Color(0.72f, 0.53f, 0.04f, 1f); // golden
    [SerializeField] private Color color = new Color(0.75f, 0.1f, 0.5f, 1f);
    [SerializeField] private float glowSize = 220f;   // pixels (1080x1920 reference par)
    [SerializeField] private float ringSize = 300f;
    [SerializeField] private float duration = 0.45f;

    private Canvas canvas;
    private Sprite glowSprite;
    private Sprite ringSprite;

    // Koi scene setup nahi chahiye: game start hote hi ye khud ban jata hai
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (instance != null) return;
        new GameObject("TouchEffect").AddComponent<TouchEffect>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        BuildCanvas();
        glowSprite = MakeSprite(false);
        ringSprite = MakeSprite(true);
    }

    private void Update()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began)
                Spawn(t.position);
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
            Spawn(Input.mousePosition);
#endif
    }

    // ---------- Setup ----------

    private void BuildCanvas()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // sab UI ke upar

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        // GraphicRaycaster nahi lagaya, is liye buttons block nahi hote
    }

    private Sprite MakeSprite(bool ring)
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c; // 0 centre, 1 edge
                float a;
                if (ring)
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.9f) / 0.07f); // patli ring
                else
                    a = d >= 1f ? 0f : Mathf.Pow(1f - d, 2f);             // soft glow
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // ---------- Effect ----------

    private void Spawn(Vector2 screenPos)
    {
        Image glow = CreateImage(glowSprite, screenPos);
        Image ring = CreateImage(ringSprite, screenPos);
        StartCoroutine(Animate(glow, glowSize, 0.3f, 1.1f, 1f));
        StartCoroutine(Animate(ring, ringSize, 0.2f, 1.0f, 0.8f));
    }

    private Image CreateImage(Sprite sprite, Vector2 screenPos)
    {
        var go = new GameObject("fx", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        go.transform.position = screenPos; // Overlay canvas mein position = screen pixels

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private IEnumerator Animate(Image img, float size, float startScale, float endScale, float startAlpha)
    {
        RectTransform rt = img.rectTransform;
        rt.sizeDelta = new Vector2(size, size);

        float t = 0f;
        while (t < duration && img != null)
        {
            float p = t / duration;
            float eased = 1f - (1f - p) * (1f - p); // ease-out

            rt.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, eased);
            img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(startAlpha, 0f, p));

            t += Time.unscaledDeltaTime; // pause ke waqt bhi chale
            yield return null;
        }

        if (img != null) Destroy(img.gameObject);
    }
}