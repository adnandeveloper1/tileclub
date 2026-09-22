using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Win panel ke andar wale "LEVEL X" button ke liye: panel active hone ke
/// kuch second baad (delayBeforeChange), ye button khud current completed
/// level number dikhata hai, phir bounce (scale overshoot) ke sath text ko
/// agle unlocked level number mein badal deta hai — bilkul reference video
/// jaisa.
///
/// SELF-TRIGGERING: Isse kisi se bhi call karne ki zaroorat nahi. Jaise hi
/// ye GameObject active hota hai (jab win panel dikhta hai), ye khud
/// sequence chala deta hai.
///
/// Attach: Win panel ke andar us "LEVEL X" button GameObject par.
/// </summary>
public class LevelButtonBounceEffect : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Khali chhodo to GameManager.Instance khud use ho jayega.")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_Text buttonLabel;
    [Tooltip("Button ka RectTransform jispe bounce scale animation chalegi. Khali chhodo to isi GameObject ka RectTransform use hoga.")]
    [SerializeField] private RectTransform buttonRect;
    [SerializeField] private SprinkleBurstEffect sprinkleBurstEffect;

    [Header("Timing")]
    [Tooltip("Panel active hone ke kitne second baad button khud dikhna (pop-in) shuru ho.")]
    [SerializeField] private float appearDelay = 0.5f;
    [Tooltip("Button pop-in (0 se normal size tak) hone mein kitna time lage.")]
    [SerializeField] private float appearDuration = 0.3f;
    [Tooltip("Panel dikhne ke kitne second baad ye poora effect shuru ho.")]
    [SerializeField] private float delayBeforeChange = 2f;
    [SerializeField] private float bounceDuration = 0.35f;
    [Tooltip("1 = normal size. 1.15 = 15% bada overshoot.")]
    [SerializeField] private float overshootScale = 1.15f;

    private Vector3 restScale;
    private bool hasPlayedThisActivation;

    private void Awake()
    {
        buttonRect = buttonRect != null ? buttonRect : GetComponent<RectTransform>();
        sprinkleBurstEffect = sprinkleBurstEffect != null ? sprinkleBurstEffect : GetComponent<SprinkleBurstEffect>();
        restScale = buttonRect != null ? buttonRect.localScale : Vector3.one;

        if (buttonRect != null)
            buttonRect.localScale = Vector3.zero;
    }

    private void OnEnable()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (hasPlayedThisActivation)
            return;

        hasPlayedThisActivation = true;
        StartCoroutine(PlaySequence());
    }

    private void OnDisable()
    {
        hasPlayedThisActivation = false;

        if (buttonRect != null)
            buttonRect.localScale = Vector3.zero;
    }

    private IEnumerator PlaySequence()
    {
        int currentLevel = gameManager != null ? gameManager.GetCurrentLevelNumber() : 0;
        int nextLevel = currentLevel + 1;

        if (buttonLabel != null)
            buttonLabel.text = currentLevel.ToString();

        // Button khud pop-in hone se pehle chhupa rahega (scale 0).
        yield return new WaitForSeconds(appearDelay);

        float appearElapsed = 0f;
        while (appearElapsed < appearDuration)
        {
            appearElapsed += Time.deltaTime;
            float appearProgress = Mathf.Clamp01(appearElapsed / appearDuration);
            float appearEased = EaseOutBack(appearProgress);

            if (buttonRect != null)
                buttonRect.localScale = Vector3.LerpUnclamped(Vector3.zero, restScale, appearEased);

            yield return null;
        }

        if (buttonRect != null)
            buttonRect.localScale = restScale;

        yield return new WaitForSeconds(delayBeforeChange);

        // Bounce ke "peak" (sabse chhota → sabse bada) point par text badal do,
        // taake number-change aur visual pop ek sath mehsoos ho.
        bool textSwapped = false;
        float elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / bounceDuration);
            float eased = EaseOutBack(progress);

            if (buttonRect != null)
                buttonRect.localScale = Vector3.LerpUnclamped(restScale, restScale * overshootScale, eased);

            if (!textSwapped && progress >= 0.35f)
            {
                textSwapped = true;

                if (buttonLabel != null)
                    buttonLabel.text = nextLevel.ToString();

                if (sprinkleBurstEffect != null)
                    sprinkleBurstEffect.PlayBurst();
            }

            yield return null;
        }

        // Overshoot se wapas normal scale par settle karo.
        elapsed = 0f;
        float settleDuration = bounceDuration * 0.6f;
        Vector3 fromScale = buttonRect != null ? buttonRect.localScale : restScale;
        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / settleDuration);

            if (buttonRect != null)
                buttonRect.localScale = Vector3.Lerp(fromScale, restScale, progress);

            yield return null;
        }

        if (buttonRect != null)
            buttonRect.localScale = restScale;
    }

    private float EaseOutBack(float progress)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        float adjusted = progress - 1f;
        return 1f + c3 * adjusted * adjusted * adjusted + c1 * adjusted * adjusted;
    }

}