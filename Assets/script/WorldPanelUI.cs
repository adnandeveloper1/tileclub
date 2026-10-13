using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Put this on the world panel prefab (the root object).
public class WorldPanelUI : MonoBehaviour
{
    [Header("Identity: tells the game which world this panel is")]
    public string worldName;      // e.g. "Japan"
    public int firstLevel = 1;    // first level number in this world
    public int lastLevel = 10;    // last level number in this world

    [Header("Optional visuals (leave empty if your art already has the text)")]
    public Image backgroundImage;
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    public Transform levelGridParent;   // the "LevelGrid" child

    // True if the given level number belongs to this world.
    public bool ContainsLevel(int levelNumber)
    {
        return levelNumber >= firstLevel && levelNumber <= lastLevel;
    }

    // Only used by WorldListController (data-driven mode).
    public void Setup(WorldData world)
    {
        worldName = world.worldName;

        if (titleText != null) titleText.text = world.worldName;
        if (subtitleText != null) subtitleText.text = world.levelRangeText;

        if (backgroundImage != null)
        {
            if (world.backgroundSprite != null)
                backgroundImage.sprite = world.backgroundSprite;
            else
                backgroundImage.color = world.themeColor;
        }
    }
}