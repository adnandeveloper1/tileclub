using UnityEngine;

// Bridge between the level select screen and GameManager.
// GameManager is the single source of truth. The select screen only reads from it,
// and tells it which level was tapped (SetCurrentLevel).
public static class LevelProgress
{
    // Same keys GameManager saves; used only if GameManager.Instance is missing
    // (e.g. when you press Play directly from the LevelSelect scene in the editor).
    const string HighestKey = "HighestUnlockedLevel";
    const string CurrentKey = "CurrentLevelNumber";

    // Used only by the select screen so the unlock animation plays once per level.
    const string ShownKey = "LastShownUnlocked";

    public static int HighestUnlocked
    {
        get
        {
            if (GameManager.Instance != null)
                return GameManager.Instance.GetHighestUnlockedLevel();
            return PlayerPrefs.GetInt(HighestKey, 1);
        }
    }

    // Level being played. LevelButtonUI sets this when a button is tapped.
    public static int CurrentLevel
    {
        get
        {
            if (GameManager.Instance != null)
                return GameManager.Instance.GetCurrentLevelNumber();
            return PlayerPrefs.GetInt(CurrentKey, 1);
        }
        set
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCurrentLevel(value);
            }
            else
            {
                PlayerPrefs.SetInt(CurrentKey, value);
                PlayerPrefs.Save();
            }
        }
    }

    public static int LastShownUnlocked
    {
        get { return PlayerPrefs.GetInt(ShownKey, 1); }
        set { PlayerPrefs.SetInt(ShownKey, value); PlayerPrefs.Save(); }
    }

    // Testing only.
    public static void DebugUnlockNext()
    {
        int next = HighestUnlocked + 1;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnlockLevel(next);
        }
        else
        {
            PlayerPrefs.SetInt(HighestKey, next);
            PlayerPrefs.Save();
        }
    }
}