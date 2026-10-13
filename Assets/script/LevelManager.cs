using System.Linq;
using UnityEngine;

// Put this on the Canvas of the level select scene.
public class LevelManager : MonoBehaviour
{
    public Transform contentParent;          // Scroll View > Viewport > Content
    public bool unlockAllForTesting = false;

    [Header("Unlock animation timing")]
    public float firstDelay = 0.5f;          // wait before the first unlock animation
    public float stagger = 0.4f;             // gap between animations if several unlock together

    void Start()
    {
        int highest = LevelProgress.HighestUnlocked;
        int lastShown = LevelProgress.LastShownUnlocked;
        float delay = firstDelay;

        var buttons = contentParent.GetComponentsInChildren<LevelButtonUI>(true)
                                   .OrderBy(b => b.levelNumber);

        foreach (LevelButtonUI btn in buttons)
        {
            bool unlockedNow = unlockAllForTesting || btn.levelNumber <= highest;
            bool unlockedBefore = unlockAllForTesting || btn.levelNumber <= lastShown;

            if (unlockedNow && !unlockedBefore)
            {
                // newly unlocked: show locked, then animate to unlocked
                btn.PlayUnlockAnimation(delay);
                delay += stagger;
            }
            else
            {
                btn.SetLocked(!unlockedNow);
            }
        }

        LevelProgress.LastShownUnlocked = highest;
    }

    // Right-click the component header to use these while testing.
    [ContextMenu("Debug: Unlock Next Level")]
    void DebugUnlockNext()
    {
        LevelProgress.DebugUnlockNext();
    }

    // Makes the newest unlock animate again the next time the scene opens.
    [ContextMenu("Debug: Replay Last Unlock Animation")]
    void DebugReplayAnimation()
    {
        LevelProgress.LastShownUnlocked = Mathf.Max(1, LevelProgress.HighestUnlocked - 1);
    }
}