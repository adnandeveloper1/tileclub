using UnityEngine;

/// Tiny wrapper around PlayerPrefs so any script can read a setting.
/// Usage: if (SettingsStore.GetBool("sfx")) PlaySound();
public static class SettingsStore
{
    public static bool GetBool(string key, bool defaultValue = true)
        => PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;

    public static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Random 9-digit player ID, created once and remembered.
    public static string GetPlayerId()
    {
        if (!PlayerPrefs.HasKey("player_id"))
            PlayerPrefs.SetInt("player_id", Random.Range(100000000, 999999999));
        int id = PlayerPrefs.GetInt("player_id");
        return $"{id / 1000000:000} {id / 1000 % 1000:000} {id % 1000:000}";
    }
}