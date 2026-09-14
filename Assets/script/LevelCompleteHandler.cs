using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelCompleteHandler : MonoBehaviour
{
    [Tooltip("Home scene ka exact naam (Build Settings mein jo likha hai)")]
    public string homeSceneName = "Home";

    [Tooltip("Khali (0) chhodo to current scene ke naam se number nikal liya jayega (lvl3 -> 3). Warna manually set kar do.")]
    public int levelNumberOverride = 0;

    /// <summary>Apna existing win-detection code isi function ko call kare.</summary>
    public void OnLevelWon()
    {
        int currentLevelNumber = GetCurrentLevelNumber();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteLevel(currentLevelNumber);
        }
        else
        {
            Debug.LogWarning("LevelCompleteHandler: GameManager scene mein nahi mila — progress save nahi hui.");
        }

        SceneManager.LoadScene(homeSceneName);
    }

    private int GetCurrentLevelNumber()
    {
        if (levelNumberOverride > 0) return levelNumberOverride;

        string sceneName = SceneManager.GetActiveScene().name; // e.g. "lvl3"
        string numberPart = System.Text.RegularExpressions.Regex.Match(sceneName, @"\d+").Value;

        if (int.TryParse(numberPart, out int parsed))
        {
            return parsed;
        }

        Debug.LogWarning("LevelCompleteHandler: Scene name '" + sceneName + "' se level number nahi mil saka. 'levelNumberOverride' set kar do.");
        return 1;
    }
}