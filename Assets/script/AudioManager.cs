using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sound Effects")]
    [SerializeField] private AudioClip tapSound;
    [SerializeField] private AudioClip lockedLevelSound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;
    [SerializeField] private AudioClip matchSound;   // 3 tiles match hone par

    [Header("Music")]
    [SerializeField] private AudioClip gameStartMusic;   // Home / selector / baqi non-level scenes
    [SerializeField] private AudioClip levelPlayMusic;   // jin scenes ka naam "lvl" se shuru ho

    [Header("Volume")]
    [SerializeField] private float lockedBoost = 2f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.6f;

    [Header("Settings")]
    [SerializeField] private string levelScenePrefix = "lvl";

    private AudioSource sfxSource;
    private AudioSource musicSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = musicVolume;

        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        // Mobile: kahin bhi tap
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
                PlaySfx(tapSound);
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        // Editor / PC: mouse click
        if (Input.GetMouseButtonDown(0))
            PlaySfx(tapSound);
#endif
    }

    // ---------------- Music (scene ke hisab se) ----------------

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isLevel = scene.name.StartsWith(levelScenePrefix, System.StringComparison.OrdinalIgnoreCase);
        PlayMusic(isLevel ? levelPlayMusic : gameStartMusic, true);
    }

    private void PlayMusic(AudioClip clip, bool loop)
    {
        if (clip == null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            return;
        }

        // Same music pehle se chal rahi ho (jaise Home -> Select) to restart na karo
        if (musicSource.clip == clip && musicSource.isPlaying && musicSource.loop == loop)
            return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // ---------------- Dusri scripts yahan se call karengi ----------------

    /// <summary>Win par: level music ruk jayegi, win sound chalegi.</summary>
    public static void PlayWin()
    {
        if (Instance == null) return;
        Instance.PlayMusic(Instance.winSound, false);
    }

    /// <summary>Lose par: level music ruk jayegi, lose sound chalegi.</summary>
    public static void PlayLose()
    {
        if (Instance == null) return;
        Instance.PlayMusic(Instance.loseSound, false);
    }

    /// <summary>Locked level par tap karne par.</summary>
  public static void PlayLocked()
{
    if (Instance == null) return;
    if (Instance.lockedLevelSound != null)
        Instance.sfxSource.PlayOneShot(Instance.lockedLevelSound, Instance.sfxVolume * Instance.lockedBoost);
  }
    /// <summary>3 matching tiles destroy hone par.</summary>
    public static void PlayMatch()
   {
    if (Instance == null) return;
    Instance.PlaySfx(Instance.matchSound);
    }
}