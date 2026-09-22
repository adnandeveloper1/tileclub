using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Generic level-select loader: ek hi script se kitne bhi buttons handle ho
/// sakte hain. Inspector mein "Levels" array mein har entry ke liye
/// (1) us button ko drag karo, (2) us scene ka exact naam likho (jaise
/// jaisa Build Settings mein / File naam hai, e.g. "lvl1", "lvl2").
///
/// Attach: Kisi bhi ek GameObject par (e.g. "LevelButtonsManager" parent),
/// button khud is script par nahi hone chahiye — array mein reference se
/// wire hote hain.
/// </summary>
public class LevelSceneLoader : MonoBehaviour
{
    [Serializable]
    public class LevelEntry
    {
        [Tooltip("Sirf pehchan ke liye (Inspector mein readable rahe) — code mein use nahi hota.")]
        public string levelName;

        [Tooltip("Ye button click hone par sceneName load hoga.")]
        public Button button;

        [Tooltip("Exact scene ka naam jaisa Build Settings mein hai (case-sensitive), e.g. lvl1, lvl2.")]
        public string sceneName;
    }

    [Header("Levels")]
    [Tooltip("Har entry: konsa button, konsi scene load karega.")]
    [SerializeField] private LevelEntry[] levels;

    private UnityEngine.Events.UnityAction[] listeners;

    private void OnEnable()
    {
        if (levels == null) return;

        listeners = new UnityEngine.Events.UnityAction[levels.Length];

        for (int i = 0; i < levels.Length; i++)
        {
            LevelEntry entry = levels[i];
            if (entry == null || entry.button == null) continue;

            // Closure-safe capture ke liye local copy.
            string sceneToLoad = entry.sceneName;
            UnityEngine.Events.UnityAction listener = () => LoadScene(sceneToLoad);
            listeners[i] = listener;
            entry.button.onClick.AddListener(listener);
        }
    }

    private void OnDisable()
    {
        if (levels == null || listeners == null) return;

        for (int i = 0; i < levels.Length; i++)
        {
            LevelEntry entry = levels[i];
            if (entry == null || entry.button == null || listeners[i] == null) continue;

            entry.button.onClick.RemoveListener(listeners[i]);
        }
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("LevelSceneLoader: is button ke liye scene name khali hai.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}