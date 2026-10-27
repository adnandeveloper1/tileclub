using UnityEditor;
using UnityEngine;

public static class GameUnlockMenu
{
    private const string HighestUnlockedLevelKey = "HighestUnlockedLevel";
    private const int TotalLevels = 10;   // apne game ke total levels yahan likho

    [MenuItem("Edit/Unlock All Levels")]
    private static void UnlockAll()
    {
        UnlockUpTo(TotalLevels);
    }

    [MenuItem("Edit/Unlock Levels Up To...")]
    private static void OpenWindow()
    {
        UnlockWindow.Open();
    }

    public static void UnlockUpTo(int level)
    {
        level = Mathf.Clamp(level, 1, TotalLevels);

        if (Application.isPlaying && GameManager.Instance != null)
        {
            // Game chal rahi hai: sirf upar ki taraf unlock ho sakta hai
            if (level <= GameManager.Instance.HighestUnlockedLevel)
            {
                Debug.LogWarning("Level " + level + " pehle se unlocked hai. Kam karne ke liye Play mode band karke ye menu use karo.");
                return;
            }
            GameManager.Instance.UnlockLevel(level);
        }
        else
        {
            PlayerPrefs.SetInt(HighestUnlockedLevelKey, level);
            PlayerPrefs.Save();
        }

        Debug.Log("Levels 1 se " + level + " tak unlocked.");
    }
}

public class UnlockWindow : EditorWindow
{
    private int level = 3;

    public static void Open()
    {
        var w = GetWindow<UnlockWindow>(true, "Unlock Levels", true);
        w.minSize = new Vector2(260, 90);
        w.maxSize = new Vector2(260, 90);
    }

    private void OnGUI()
    {
        GUILayout.Space(8);
        level = EditorGUILayout.IntField("Unlock up to level", level);
        GUILayout.Space(6);

        if (GUILayout.Button("Unlock"))
        {
            GameUnlockMenu.UnlockUpTo(level);
            Close();
        }
    }
}