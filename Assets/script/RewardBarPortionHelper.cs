using UnityEngine;

/// <summary>
/// Computes the Fill From / Fill To fraction for ONE portion of a
/// multi-segment reward bar (e.g. a bar split into 4 equal portions, one
/// filling per level completed, resetting every 4 levels).
///
/// Call GetPortionRange(currentLevelNumber) and feed the two returned
/// floats into your existing "Fill From" / "Fill To" fields (or straight
/// into FillBarByPoints.AnimateProgress(from, to, duration)).
/// </summary>
public static class RewardBarPortionHelper
{
    /// <summary>
    /// levelNumber: the level the player just completed (1-based, e.g. 11).
    /// totalPortions: how many equal slices the bar is divided into (e.g. 4).
    /// Returns (fillFrom, fillTo) as 0–1 fractions of the WHOLE bar.
    /// </summary>
    public static (float fillFrom, float fillTo) GetPortionRange(int levelNumber, int totalPortions = 4)
    {
        int portionIndex = (levelNumber - 1) % totalPortions; // 0-based: 0,1,2,3, then wraps
        float fillFrom = (float)portionIndex / totalPortions;
        float fillTo = (float)(portionIndex + 1) / totalPortions;
        return (fillFrom, fillTo);
    }
}