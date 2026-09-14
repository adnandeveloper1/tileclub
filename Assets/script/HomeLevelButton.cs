using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class HomeLevelButton : MonoBehaviour
{
    [Tooltip("Jis TMP Text mein 'LEVEL X' dikhna hai")]
    public TMP_Text levelLabel;

    [Tooltip("Scene naming prefix — 'lvl' matlab lvl1, lvl2, lvl3...")]
    public string scenePrefix = "lvl";

    private void OnEnable()
    {
        UpdateLabel();

        // Agar GameManager baad mein level unlock kare (isi session mein), label auto update ho jaye
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelUnlocked += HandleLevelUnlocked;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelUnlocked -= HandleLevelUnlocked;
        }
    }

    private void HandleLevelUnlocked(int newLevel) => UpdateLabel();

    private void UpdateLabel()
    {
        int levelToPlay = GameManager.Instance != null ? GameManager.Instance.GetHighestUnlockedLevel() : 1;
        if (levelLabel != null)
        {
            levelLabel.text = "" + levelToPlay;
        }
    }

    /// <summary>Button OnClick() mein isi function ko wire karo.</summary>
    public void PlayCurrentLevel()
    {
        int levelToPlay = GameManager.Instance != null ? GameManager.Instance.GetHighestUnlockedLevel() : 1;
        string sceneName = scenePrefix + levelToPlay;
        SceneManager.LoadScene(sceneName);
    }
}