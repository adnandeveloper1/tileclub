using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackButtonAutoHide : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private CanvasGroup backButtonGroup;

    [Tooltip("Other UI objects (e.g. the TileClub logo) that should hide and show together with the back button. A CanvasGroup is added automatically if they don't have one.")]
    [SerializeField] private GameObject[] extraTargets;

    [Tooltip("Top se itne pixels tak scroll hone par button dikhta rahe")]
    [SerializeField] private float showWithinPixels = 40f;

    [SerializeField] private float fadeSpeed = 8f;

    private CanvasGroup[] groups;
    private bool shouldShow = true;

    private void Awake()
    {
        var list = new List<CanvasGroup>();

        if (backButtonGroup != null)
            list.Add(backButtonGroup);

        if (extraTargets != null)
        {
            foreach (GameObject target in extraTargets)
            {
                if (target == null) continue;

                CanvasGroup group = target.GetComponent<CanvasGroup>();
                if (group == null)
                    group = target.AddComponent<CanvasGroup>();

                list.Add(group);
            }
        }

        groups = list.ToArray();
    }

    private void Start()
    {
        scrollRect.onValueChanged.AddListener(OnScroll);
        OnScroll(scrollRect.normalizedPosition);
    }

    private void OnDestroy()
    {
        if (scrollRect != null)
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

        foreach (CanvasGroup group in groups)
        {
            group.alpha = Mathf.MoveTowards(
                group.alpha, target, fadeSpeed * Time.unscaledDeltaTime);

            // Chhupa hua object click na ho
            group.interactable = shouldShow;
            group.blocksRaycasts = shouldShow;
        }
    }
}