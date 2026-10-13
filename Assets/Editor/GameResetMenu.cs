using UnityEditor;
using UnityEngine;

public static class GameResetMenu
{
    private const string SoundMutedKey = "SoundMuted";

    [MenuItem("Edit/Reset Game Progress")]
    private static void ResetGameProgress()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Reset Game Progress",
            "Poori game reset ho jayegi:\n\n" +
            "• Coins = 0\n" +
            "• Wisdom = starting value (83)\n" +
            "• Level 1 se start\n" +
            "• Sirf Level 1 unlocked\n\n" +
            "Kya aap sure hain?",
            "Haan, Reset karo", "Cancel");

        if (!confirm) return;

        if (Application.isPlaying && GameManager.Instance != null)
        {
            // Game chal rahi hai: GameManager ki memory bhi reset hogi
            GameManager.Instance.ResetAllProgress(true);
        }
        else
        {
            // Game band hai: seedha saved data delete (sound setting bacha ke)
            int muted = PlayerPrefs.GetInt(SoundMutedKey, 0);
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetInt(SoundMutedKey, muted);
            PlayerPrefs.Save();
        }

        Debug.Log("Game progress reset ho gayi: Coins 0, Wisdom 83, Level 1 only unlocked.");
    }
}