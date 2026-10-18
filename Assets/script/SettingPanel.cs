using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// Root of the Settings screen. Needs a CanvasGroup on the same object.
[RequireComponent(typeof(CanvasGroup))]
public class SettingPanel : MonoBehaviour
{
    [Header("Footer")]
    [SerializeField] TMP_Text versionText;
    [SerializeField] TMP_Text playerIdText;

    [Header("Apply settings (optional)")]
    [SerializeField] AudioSource music;

    [Header("Button actions (wire in Inspector)")]
    public UnityEvent onScoreSystem, onIntroAnimation, onLanguage,
                      onSupport, onPrivacy, onTerms, onEditTileset, onGoogleSignIn;

    CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (versionText)  versionText.text  = "v " + Application.version;
        if (playerIdText) playerIdText.text = "ID: " + SettingsStore.GetPlayerId();
        if (music) music.mute = !SettingsStore.GetBool("music");
    }

    void OnEnable()  => SettingToggle.OnChanged += Apply;
    void OnDisable() => SettingToggle.OnChanged -= Apply;

    // React to toggles right away
    void Apply(string key, bool on)
    {
        if (key == "music" && music) music.mute = !on;
#if UNITY_ANDROID || UNITY_IOS
        if (key == "vibration" && on) Handheld.Vibrate();   // little feedback buzz
#endif
    }

    // Hook these to the gear button and back arrow
    public void Open()  { gameObject.SetActive(true); StartCoroutine(Fade(0, 1, false)); }
    public void Close() { StartCoroutine(Fade(1, 0, true)); }

    IEnumerator Fade(float from, float to, bool disableAtEnd)
    {
        group.interactable = false;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.18f;
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        group.alpha = to;
        group.interactable = true;
        if (disableAtEnd) gameObject.SetActive(false);
    }
}