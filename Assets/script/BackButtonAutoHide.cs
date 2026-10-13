using UnityEngine;
using UnityEngine.UI;

public class BackButtonAutoHide : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private CanvasGroup backButtonGroup;

    [Tooltip("Top se itne pixels tak scroll hone par button dikhta rahe")]
    [SerializeField] private float showWithinPixels = 40f;

    [SerializeField] private float fadeSpeed = 8f;

    private bool shouldShow = true;

    private void Start()
    {
        scrollRect.onValueChanged.AddListener(OnScroll);
        OnScroll(scrollRect.normalizedPosition);
    }

    private void OnDestroy()
    {
        scrollRect.onValueChanged.RemoveListener(OnScroll);
    }

    private void OnScroll(Vector2 _)
    {
        // Content ka top se kitna neeche scroll hua (pixels mein)
        float scrolled = scrollRect.content.anchoredPosition.y;
        shouldShow = scrolled <= showWithinPixels;
    }

    private void Update()
    {
        float target = shouldShow ? 1f : 0f;
        backButtonGroup.alpha = Mathf.MoveTowards(
            backButtonGroup.alpha, target, fadeSpeed * Time.unscaledDeltaTime);

        // Chhupa hua button click na ho
        backButtonGroup.interactable = shouldShow;
        backButtonGroup.blocksRaycasts = shouldShow;
    }
}