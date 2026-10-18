using UnityEngine;
using UnityEngine.UI;

/// Put on a full-screen UI Image. Builds a smooth top-to-bottom gradient.
[RequireComponent(typeof(Image))]
public class GradientBackground : MonoBehaviour
{
    [SerializeField] Color top    = new Color32(0x2F, 0x7B, 0xE8, 255); // deeper blue
    [SerializeField] Color bottom = new Color32(0x5C, 0xBE, 0xF0, 255); // light sky blue

    void Awake()
    {
        var tex = new Texture2D(1, 2, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        tex.SetPixel(0, 0, bottom);
        tex.SetPixel(0, 1, top);
        tex.Apply();
        GetComponent<Image>().sprite =
            Sprite.Create(tex, new Rect(0, 0, 1, 2), new Vector2(0.5f, 0.5f));
    }
}