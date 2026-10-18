using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

// Put this on each level button (the object with the Button component).
public class LevelButtonUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Required")]
    public Button button;
    public Image buttonImage;          // the Image that shows the locked / unlocked art

    [Header("The two images")]
    public Sprite lockedSprite;
    public Sprite unlockedSprite;

    [Header("Optional (leave empty if not needed)")]
    public TMP_Text levelNumberText;
    public GameObject lockIcon;
    public GameObject unlockEffect;    // a glow / particle object, shown briefly on unlock
    public Image thumbnail;

    [Header("Set on each button in the scene")]
    public int levelNumber = 1;
    public string sceneName;

    [Header("Unlock animation")]
    public float popScale = 1.25f;

    [Header("Locked tap shake")]
    public float lockedShakeAngle = 5f;       // how far it tilts (degrees)
    public float lockedShakeDuration = 0.3f;  // how long the shake lasts (seconds)

    private bool isLocked = true;
    private Coroutine anim;
    private Coroutine shakeAnim;

    void OnValidate()
    {
        if (levelNumberText != null)
            levelNumberText.text = levelNumber.ToString();
    }

    // Used only by WorldListController (data-driven mode), safe to leave.
    public void Setup(LevelData level, bool locked)
    {
        levelNumber = level.levelNumber;
        sceneName = level.sceneName;
        if (levelNumberText != null) levelNumberText.text = levelNumber.ToString();
        if (thumbnail != null && level.thumbnailSprite != null)
            thumbnail.sprite = level.thumbnailSprite;
        SetLocked(locked);
    }

    // Instantly shows the locked or unlocked look.
    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (buttonImage != null)
        {
            Sprite s = locked ? lockedSprite : unlockedSprite;
            if (s != null) buttonImage.sprite = s;
        }

        if (thumbnail != null) thumbnail.gameObject.SetActive(!locked);
        if (levelNumberText != null) levelNumberText.gameObject.SetActive(!locked);
        if (lockIcon != null) lockIcon.SetActive(locked);
        if (unlockEffect != null) unlockEffect.SetActive(false);

        button.interactable = !locked;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);
    }

    // Shows locked first, then shakes, swaps the image and pops.
    public void PlayUnlockAnimation(float delay)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(UnlockRoutine(delay));
    }

    private IEnumerator UnlockRoutine(float delay)
    {
        SetLocked(true);
        yield return new WaitForSecondsRealtime(delay);

        Transform t = transform;
        Vector3 baseScale = t.localScale;

        // 1) shake the locked button
        float time = 0f;
        while (time < 0.3f)
        {
            time += Time.unscaledDeltaTime;
            float z = Mathf.Sin(time * 60f) * 6f * (1f - time / 0.3f);
            t.localRotation = Quaternion.Euler(0f, 0f, z);
            yield return null;
        }
        t.localRotation = Quaternion.identity;

        // 2) swap to the unlocked image and pop
        SetLocked(false);
        if (unlockEffect != null) unlockEffect.SetActive(true);

        time = 0f;
        while (time < 0.35f)
        {
            time += Time.unscaledDeltaTime;
            float p = time / 0.35f;
            float s = 1f + (popScale - 1f) * Mathf.Sin(p * Mathf.PI);   // 1 -> popScale -> 1
            t.localScale = baseScale * s;
            yield return null;
        }
        t.localScale = baseScale;

        if (unlockEffect != null)
        {
            yield return new WaitForSecondsRealtime(0.8f);
            unlockEffect.SetActive(false);
        }
    }

    // A locked button has interactable = false, so Button.onClick never fires for it.
    // This catches the tap directly so we can shake the locked level.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isLocked) return;   // unlocked buttons are handled by Button.onClick
        PlayLockedShake();
    }

    public void PlayLockedShake()
    {
        AudioManager.PlayLocked();// play sound when player tap on locked level
        if (shakeAnim != null) StopCoroutine(shakeAnim);
        shakeAnim = StartCoroutine(LockedShakeRoutine());
    }

    private IEnumerator LockedShakeRoutine()
    {
        Transform t = transform;
        float time = 0f;
        while (time < lockedShakeDuration)
        {
            time += Time.unscaledDeltaTime;
            float z = Mathf.Sin(time * 60f) * lockedShakeAngle * (1f - time / lockedShakeDuration);
            t.localRotation = Quaternion.Euler(0f, 0f, z);
            yield return null;
        }
        t.localRotation = Quaternion.identity;
    }

    private void OnClicked()
    {
        if (isLocked) return;                       // locked levels never load a scene
        if (string.IsNullOrEmpty(sceneName)) return;

        LevelProgress.CurrentLevel = levelNumber;   // so the game scene knows which level is being played
        SceneManager.LoadScene(sceneName);
    }
}