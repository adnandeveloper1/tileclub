using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the loading-screen progress bar. Works in two modes:
///
///  A) Simulated loading (default) — fills 0 -> 1 over `simulatedDuration`
///     seconds, then invokes `onLoadingComplete` (hook this up to show your
///     main menu / start button in the Inspector).
///
///  B) Real scene load — set `loadSceneOnComplete = true` and type the
///     scene name in `sceneToLoad`. The bar will then reflect Unity's
///     actual async scene-loading progress instead of a fake timer.
/// </summary>
public class LoadingBarController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Image with Image Type = Filled that visually represents progress.")]
    [SerializeField] private Image fillImage;

    [Header("Mode")]
    [SerializeField] private bool loadSceneOnComplete = false;
    [SerializeField] private string sceneToLoad = "GameScene";

    [Header("Timing")]
    [Tooltip("Used in simulated mode, and as a MINIMUM time in real-load mode so the bar never just flashes full instantly.")]
    [SerializeField] private float simulatedDuration = 2.5f;

    [Tooltip("Higher = fill catches up to target progress faster (snappier). Lower = smoother/slower catch-up.")]
    [SerializeField] private float fillSmoothSpeed = 1.5f;

    [Header("Events")]
    public UnityEvent onLoadingComplete;

    private float displayedProgress = 0f;

    private void Start()
    {
        if (fillImage == null)
        {
            Debug.LogError("LoadingBarController: fillImage is not assigned in the Inspector.");
            return;
        }

        fillImage.fillAmount = 0f;

        if (loadSceneOnComplete)
            StartCoroutine(RealSceneLoadRoutine());
        else
            StartCoroutine(SimulatedLoadRoutine());
    }

    /// <summary>Fake progress bar: just fills up smoothly over `simulatedDuration` seconds.</summary>
    private IEnumerator SimulatedLoadRoutine()
    {
        float elapsed = 0f;

        while (elapsed < simulatedDuration)
        {
            elapsed += Time.deltaTime;
            float target = Mathf.Clamp01(elapsed / simulatedDuration);

            displayedProgress = Mathf.MoveTowards(displayedProgress, target, fillSmoothSpeed * Time.deltaTime);
            fillImage.fillAmount = displayedProgress;
            yield return null;
        }

        // Make sure it visually lands exactly on full before continuing.
        fillImage.fillAmount = 1f;
        onLoadingComplete?.Invoke();
    }

    /// <summary>Real progress bar: reflects Unity's actual async scene load.</summary>
    private IEnumerator RealSceneLoadRoutine()
    {
        float elapsed = 0f;
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f || elapsed < simulatedDuration)
        {
            elapsed += Time.deltaTime;

            // Unity reports 0 -> 0.9 while loading; the final 0.9 -> 1 only
            // happens once we allow scene activation, so we remap to 0-1 here.
            float realProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float timeProgress = Mathf.Clamp01(elapsed / simulatedDuration);
            float target = Mathf.Min(realProgress, timeProgress);

            displayedProgress = Mathf.MoveTowards(displayedProgress, target, fillSmoothSpeed * Time.deltaTime);
            fillImage.fillAmount = displayedProgress;
            yield return null;
        }

        // Smoothly finish the last stretch to 100% before switching scenes.
        while (displayedProgress < 1f)
        {
            displayedProgress = Mathf.MoveTowards(displayedProgress, 1f, fillSmoothSpeed * Time.deltaTime);
            fillImage.fillAmount = displayedProgress;
            yield return null;
        }

        onLoadingComplete?.Invoke();
        operation.allowSceneActivation = true;
    }
}