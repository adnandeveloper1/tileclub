using UnityEngine;
using UnityEngine.SceneManagement;
public class ButtonActions : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("Exact name of your Home scene (as added in Build Settings)")]
    [SerializeField] private string homeSceneName = "Home";

    [Tooltip("Exact name of your Game/Play scene (as added in Build Settings)")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Tutorial Popup")]
    [Tooltip("Wo panel jisme tutorial video/content hota hai")]
    [SerializeField] private GameObject tutorialPopupPanel;

    private const string SkipTutorialKey = "SkipTutorialOnReplay";

    private void Start()
    {
        // Scene load hote hi check karo: replay se aaye hain ya fresh start hai
        bool skipTutorial = PlayerPrefs.GetInt(SkipTutorialKey, 0) == 1;

        if (tutorialPopupPanel != null)
        {
            if (skipTutorial)
            {
                tutorialPopupPanel.SetActive(false);
            }
        }
    }

    public static bool ConsumeReplayTutorialSkip()
    {
        bool shouldSkip = PlayerPrefs.GetInt(SkipTutorialKey, 0) == 1;
        if (shouldSkip)
        {
            PlayerPrefs.SetInt(SkipTutorialKey, 0);
            PlayerPrefs.Save();
        }

        return shouldSkip;
    }

    /// <summary>
    /// Home button OnClick() se wire karo.
    /// </summary>
    public void OnHomeButtonPressed()
    {
        // Home jaate waqt flag clear rakho taake next fresh game mein tutorial dikhe
        PlayerPrefs.SetInt(SkipTutorialKey, 0);
        PlayerPrefs.Save();

        SceneManager.LoadScene(homeSceneName);
    }

    /// <summary>
    /// Replay button OnClick() se wire karo.
    /// </summary>
    public void OnReplayButtonPressed()
    {
        // Flag set karo taake reload hone par tutorial panel skip ho
        PlayerPrefs.SetInt(SkipTutorialKey, 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}