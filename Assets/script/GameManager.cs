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
    private const string CurrentLevelNumberKey = "CurrentLevelNumber";
    private const string CoinsKey = "PlayerCoins";
    private const string WisdomKey = "PlayerWisdom";
    private const string PreviousWisdomKey = "PreviousPlayerWisdom";
    private const string LastRewardedLevelKey = "LastRewardedWisdomLevel";
    private const string LastRewardedCoinLevelKey = "LastRewardedCoinLevel";
    private const string SoundMutedKey = "SoundMuted";
    private const int InitialWisdom = 83;

    public int Coins { get; private set; }
    public int Wisdom { get; private set; }
    public int PreviousWisdom { get; private set; }
    public int LastRewardedLevel { get; private set; }
    public int LastRewardedCoinLevel { get; private set; }
    public bool IsSoundMuted { get; private set; }
    public int HighestUnlockedLevel { get; private set; }

    [Header("Wisdom reward per level")]
    [SerializeField] private int[] wisdomRewardsByLevel = { 13, 6 };

    [Header("Coin reward per level")]
    [Tooltip("Level 1 ka reward pehle number par, level 2 ka doosre number par, and so on. Last value baqi levels ke liye reuse hogi.")]
    [SerializeField] private int[] coinRewardsByLevel = { 0 };

    /// <summary>The level the player is currently on / most recently completed. Used for things like the reward-bar portion calculation.</summary>
    public int CurrentLevelNumber { get; private set; }

    public event Action<int> OnCoinsChanged;
    public event Action<int> OnWisdomChanged;
    public event Action<bool> OnSoundToggled;
    public event Action<int> OnLevelUnlocked;
    public event Action<int> OnCurrentLevelChanged;

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

        if (wisdomRewardsByLevel == null || wisdomRewardsByLevel.Length == 0)
            wisdomRewardsByLevel = new[] { 13, 6 };

        if (coinRewardsByLevel == null || coinRewardsByLevel.Length == 0)
            coinRewardsByLevel = new[] { 0 };

        LoadAll();
    }

    private void LoadAll()
    {
        HighestUnlockedLevel = PlayerPrefs.GetInt(HighestUnlockedLevelKey, 1);
        CurrentLevelNumber = PlayerPrefs.GetInt(CurrentLevelNumberKey, 1);
        Coins = PlayerPrefs.GetInt(CoinsKey, 0);
        Wisdom = PlayerPrefs.GetInt(WisdomKey, InitialWisdom);
        PreviousWisdom = PlayerPrefs.GetInt(PreviousWisdomKey, Wisdom);
        LastRewardedLevel = PlayerPrefs.GetInt(LastRewardedLevelKey, 0);
        LastRewardedCoinLevel = PlayerPrefs.GetInt(LastRewardedCoinLevelKey, 0);
        IsSoundMuted = PlayerPrefs.GetInt(SoundMutedKey, 0) == 1;

        AudioListener.volume = IsSoundMuted ? 0f : 1f;
    }

    // ---------------- Level Progress ----------------

    public int GetHighestUnlockedLevel() => HighestUnlockedLevel;

    public int GetCurrentLevelNumber() => CurrentLevelNumber;

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

    /// <summary>Saves which level the player is currently on / just completed. Call this any time it changes (e.g. picking a level from the map, or right when a level completes).</summary>
    public void SetCurrentLevel(int levelNumber)
    {
        if (levelNumber == CurrentLevelNumber) return;

        CurrentLevelNumber = levelNumber;
        PlayerPrefs.SetInt(CurrentLevelNumberKey, CurrentLevelNumber);
        PlayerPrefs.Save();
        OnCurrentLevelChanged?.Invoke(CurrentLevelNumber);
    }

    /// <summary>Level complete hone pe call karo — current level save hota hai, agla level automatically unlock ho jayega.</summary>
    public void CompleteLevel(int completedLevelNumber)
    {
        if (completedLevelNumber > CurrentLevelNumber)
            SetCurrentLevel(completedLevelNumber);

        UnlockLevel(completedLevelNumber + 1);
    }

    // ---------------- Wisdom ----------------

    /// <summary>Adds wisdom, stores the value from before the reward, and saves both totals.</summary>
    public int AddWisdom(int amount)
    {
        PreviousWisdom = Wisdom;
        PlayerPrefs.SetInt(PreviousWisdomKey, PreviousWisdom);

        if (amount == 0)
        {
            PlayerPrefs.Save();
            return Wisdom;
        }

        Wisdom = Mathf.Max(0, Wisdom + amount);
        PlayerPrefs.SetInt(WisdomKey, Wisdom);
        PlayerPrefs.Save();
        OnWisdomChanged?.Invoke(Wisdom);
        return Wisdom;
    }

    /// <summary>Rewards wisdom only the first time a level is completed.</summary>
    public bool TryAwardWisdomForLevel(int levelNumber, out int wisdomBefore, out int wisdomAfter)
    {
        wisdomBefore = Wisdom;
        wisdomAfter = Wisdom;

        if (levelNumber <= 0 || levelNumber <= LastRewardedLevel)
            return false;

        int reward = GetWisdomReward(levelNumber);
        PreviousWisdom = Wisdom;
        Wisdom = Mathf.Max(0, Wisdom + reward);
        LastRewardedLevel = levelNumber;

        PlayerPrefs.SetInt(PreviousWisdomKey, PreviousWisdom);
        PlayerPrefs.SetInt(WisdomKey, Wisdom);
        PlayerPrefs.SetInt(LastRewardedLevelKey, LastRewardedLevel);
        PlayerPrefs.Save();

        wisdomAfter = Wisdom;
        OnWisdomChanged?.Invoke(Wisdom);
        return true;
    }

    private int GetWisdomReward(int levelNumber)
    {
        if (wisdomRewardsByLevel == null || wisdomRewardsByLevel.Length == 0)
            return 0;

        int rewardIndex = Mathf.Min(levelNumber - 1, wisdomRewardsByLevel.Length - 1);
        return Mathf.Max(0, wisdomRewardsByLevel[rewardIndex]);
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

    /// <summary>Rewards coins only the first time a level is completed.</summary>
    public bool TryAwardCoinsForLevel(int levelNumber, out int coinsBefore, out int coinsAfter)
    { 
        coinsBefore = Coins;
        coinsAfter = Coins;

        if (levelNumber <= 0 || levelNumber <= LastRewardedCoinLevel)
            return false;

        int reward = GetCoinReward(levelNumber);
        Debug.Log($"[CoinReward] level={levelNumber}, reward={reward}, coinsBefore={Coins}, lastRewarded={LastRewardedCoinLevel}");
        Coins = Mathf.Max(0, Coins + reward);
        LastRewardedCoinLevel = levelNumber;

        PlayerPrefs.SetInt(CoinsKey, Coins);
        PlayerPrefs.SetInt(LastRewardedCoinLevelKey, LastRewardedCoinLevel);
        PlayerPrefs.Save();

        coinsAfter = Coins;
        OnCoinsChanged?.Invoke(Coins);
        return true;
    }

    private int GetCoinReward(int levelNumber)
    {
        if (coinRewardsByLevel == null || coinRewardsByLevel.Length == 0)
            return 0;

        int rewardIndex = Mathf.Min(levelNumber - 1, coinRewardsByLevel.Length - 1);
        return Mathf.Max(0, coinRewardsByLevel[rewardIndex]);
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

    // ---------------- Reset ----------------

/// <summary>Poori game ko fresh-install state par le aata hai.
/// Coins 0, Wisdom 83 (InitialWisdom), Level 1 start, sirf Level 1 unlocked.</summary>
public void ResetAllProgress(bool keepSoundSetting = true)
{
    bool wasMuted = IsSoundMuted;

    PlayerPrefs.DeleteAll();

    Coins = 0;
    Wisdom = InitialWisdom;
    PreviousWisdom = InitialWisdom;
    LastRewardedLevel = 0;
    LastRewardedCoinLevel = 0;
    HighestUnlockedLevel = 1;
    CurrentLevelNumber = 1;
    IsSoundMuted = keepSoundSetting ? wasMuted : false;

    PlayerPrefs.SetInt(HighestUnlockedLevelKey, HighestUnlockedLevel);
    PlayerPrefs.SetInt(CurrentLevelNumberKey, CurrentLevelNumber);
    PlayerPrefs.SetInt(CoinsKey, Coins);
    PlayerPrefs.SetInt(WisdomKey, Wisdom);
    PlayerPrefs.SetInt(PreviousWisdomKey, PreviousWisdom);
    PlayerPrefs.SetInt(LastRewardedLevelKey, LastRewardedLevel);
    PlayerPrefs.SetInt(LastRewardedCoinLevelKey, LastRewardedCoinLevel);
    PlayerPrefs.SetInt(SoundMutedKey, IsSoundMuted ? 1 : 0);
    PlayerPrefs.Save();

    AudioListener.volume = IsSoundMuted ? 0f : 1f;

    OnCoinsChanged?.Invoke(Coins);
    OnWisdomChanged?.Invoke(Wisdom);
    OnCurrentLevelChanged?.Invoke(CurrentLevelNumber);
    OnSoundToggled?.Invoke(IsSoundMuted);
}
}