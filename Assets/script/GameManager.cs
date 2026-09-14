using System;
using UnityEngine;

/// <summary>
/// Central singleton. Ek hi GameObject pe laga do (e.g. Home scene mein "GameManager" naam ka empty object),
/// aur DontDestroyOnLoad ki wajah se ye har scene mein persist karega.
/// Coins, Sound aur Level Progress teeno yahin se control hote hain.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const string HighestUnlockedLevelKey = "HighestUnlockedLevel";
    private const string CoinsKey = "PlayerCoins";
    private const string SoundMutedKey = "SoundMuted";

    public int Coins { get; private set; }
    public bool IsSoundMuted { get; private set; }
    public int HighestUnlockedLevel { get; private set; }

    public event Action<int> OnCoinsChanged;
    public event Action<bool> OnSoundToggled;
    public event Action<int> OnLevelUnlocked;

    private void Awake()
    {
        // Agar scene reload hone pe dusra GameManager aa jaye to usse hata do (singleton).
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAll();
    }

    private void LoadAll()
    {
        HighestUnlockedLevel = PlayerPrefs.GetInt(HighestUnlockedLevelKey, 1);
        Coins = PlayerPrefs.GetInt(CoinsKey, 0);
        IsSoundMuted = PlayerPrefs.GetInt(SoundMutedKey, 0) == 1;

        AudioListener.volume = IsSoundMuted ? 0f : 1f;
    }

    // ---------------- Level Progress ----------------

    public int GetHighestUnlockedLevel() => HighestUnlockedLevel;

    public void UnlockLevel(int levelNumber)
    {
        if (levelNumber > HighestUnlockedLevel)
        {
            HighestUnlockedLevel = levelNumber;
            PlayerPrefs.SetInt(HighestUnlockedLevelKey, HighestUnlockedLevel);
            PlayerPrefs.Save();
            OnLevelUnlocked?.Invoke(HighestUnlockedLevel);
        }
    }

    /// <summary>Level complete hone pe call karo — agla level automatically unlock ho jayega.</summary>
    public void CompleteLevel(int completedLevelNumber)
    {
        UnlockLevel(completedLevelNumber + 1);
    }

    // ---------------- Coins ----------------

    public void AddCoins(int amount)
    {
        if (amount == 0) return;
        Coins = Mathf.Max(0, Coins + amount);
        PlayerPrefs.SetInt(CoinsKey, Coins);
        PlayerPrefs.Save();
        OnCoinsChanged?.Invoke(Coins);
    }

    /// <returns>true agar spend ho gaye, false agar coins kam the.</returns>
    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || Coins < amount) return false;
        Coins -= amount;
        PlayerPrefs.SetInt(CoinsKey, Coins);
        PlayerPrefs.Save();
        OnCoinsChanged?.Invoke(Coins);
        return true;
    }

    // ---------------- Sound ----------------

    public void SetSoundMuted(bool muted)
    {
        IsSoundMuted = muted;
        PlayerPrefs.SetInt(SoundMutedKey, muted ? 1 : 0);
        PlayerPrefs.Save();
        AudioListener.volume = muted ? 0f : 1f;
        OnSoundToggled?.Invoke(muted);
    }

    public void ToggleSound() => SetSoundMuted(!IsSoundMuted);
}