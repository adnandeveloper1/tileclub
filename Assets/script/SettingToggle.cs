using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Put this on the same GameObject as a UI Toggle (the navy "track").
/// The knob is a child Image whose anchor/pivot are centered (0.5, 0.5).
[RequireComponent(typeof(Toggle))]
public class SettingToggle : MonoBehaviour
{
    // Anyone can listen: SettingToggle.OnChanged += (key, value) => ...
    public static event Action<string, bool> OnChanged;

    [SerializeField] string key = "music";      // unique PlayerPrefs key
    [SerializeField] bool defaultValue = true;
    [SerializeField] RectTransform knob;
    [SerializeField] float padding = 6f;        // gap between knob and track edge
    [SerializeField] float slideTime = 0.12f;

    Toggle toggle;
    RectTransform track;
    Coroutine slide;

    void Start()
    {
        toggle = GetComponent<Toggle>();
        track  = (RectTransform)transform;

        bool saved = SettingsStore.GetBool(key, defaultValue);
        toggle.SetIsOnWithoutNotify(saved);
        knob.anchoredPosition = new Vector2(KnobX(saved), 0f);

        toggle.onValueChanged.AddListener(HandleToggled);
    }

    float KnobX(bool on)
    {
        float half = track.rect.width * 0.5f - knob.rect.width * 0.5f - padding;
        return on ? half : -half;
    }

    void HandleToggled(bool on)
    {
        SettingsStore.SetBool(key, on);
        if (slide != null) StopCoroutine(slide);
        slide = StartCoroutine(Slide(KnobX(on)));
        OnChanged?.Invoke(key, on);
    }

    IEnumerator Slide(float targetX)
    {
        float startX = knob.anchoredPosition.x, t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / slideTime;
            float x = Mathf.Lerp(startX, targetX, Mathf.SmoothStep(0, 1, t));
            knob.anchoredPosition = new Vector2(x, 0f);
            yield return null;
        }
        knob.anchoredPosition = new Vector2(targetX, 0f);
    }
}