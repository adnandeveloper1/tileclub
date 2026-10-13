using UnityEngine;

// Represents ONE level inside a world (e.g. Level 5 in "RISE").
[CreateAssetMenu(fileName = "NewLevel", menuName = "LevelSelect/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Identity")]
    public int levelNumber;              // shown on the button, e.g. "5"
    public string sceneName;             // scene to load when tapped, e.g. "Level_05"

    [Header("Visuals")]
    public Sprite thumbnailSprite;       // artwork shown when unlocked (can be left empty for locked-only levels)

    [Header("Optional extras")]
    public int starsEarned;              // 0-3, useful later for completed-level polish
}