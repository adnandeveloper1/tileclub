using System.Collections.Generic;
using UnityEngine;

// Represents ONE world/chapter band (e.g. "RISE", "JAPAN") that holds many levels.
[CreateAssetMenu(fileName = "NewWorld", menuName = "LevelSelect/World Data")]
public class WorldData : ScriptableObject
{
    [Header("Identity")]
    public string worldName;             // e.g. "RISE"
    public string levelRangeText;        // e.g. "Levels 13 - 80" (shown as subtitle)

    [Header("Visuals")]
    public Color themeColor = Color.white;   // used if you tint a plain background image
    public Sprite backgroundSprite;          // used instead of themeColor if you want gradient art

    [Header("Levels")]
    public List<LevelData> levels = new List<LevelData>();
}