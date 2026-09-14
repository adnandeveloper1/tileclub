using System;
using UnityEngine;

/// <summary>Every tile "flavor" that can appear on any level's board.</summary>
public enum TileIconType
{
    Apple,
    Carrot,
    Popsicle,
    Lightning,
    Cherry,
    WaterDrop,
    Flame,
    Cheese,
    Pizza,
    Tomato,
    Rose
}

[Serializable]
public struct TileIconEntry
{
    public TileIconType type;
    public Sprite sprite;
}

/// <summary>
/// Central lookup: given a TileIconType, returns the Sprite to display.
/// Create ONE of these in your project (Create > TileClub > Tile Icon
/// Database), fill in the sprite for every enum value, and reference it
/// from BoardSpawner. This way every level's data just says "Carrot"
/// instead of dragging the same sprite into every level asset by hand.
/// </summary>
[CreateAssetMenu(fileName = "TileIconDatabase", menuName = "TileClub/Tile Icon Database")]
public class TileIconDatabase : ScriptableObject
{
    public TileIconEntry[] entries;

    public Sprite GetSprite(TileIconType type)
    {
        foreach (var entry in entries)
        {
            if (entry.type == type)
                return entry.sprite;
        }
        Debug.LogWarning($"TileIconDatabase: no sprite assigned for {type}");
        return null;
    }
}