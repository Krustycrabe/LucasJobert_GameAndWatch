using System;
using UnityEngine;

/// <summary>
/// Single ScriptableObject that holds every obstacle pattern.
/// Each pattern is defined inline — no separate asset per pattern.
///
/// Create via right-click > VirusSplit > Obstacle Pattern Library.
/// </summary>
[CreateAssetMenu(fileName = "ObstaclePatternLibrary", menuName = "VirusSplit/Obstacle Pattern Library")]
public class ObstaclePatternLibrarySO : ScriptableObject
{
    [Tooltip("All patterns available for random selection.\n" +
             "Each pattern's 'weight' field controls its relative frequency.\n" +
             "Example : Pattern A weight=3, Pattern B weight=1 → A appears 75 % of the time.")]
    public ObstaclePatternData[] patterns;

    /// <summary>
    /// Draws one pattern using weighted random selection.
    /// Returns null if the library is empty or all weights are zero.
    /// </summary>
    public ObstaclePatternData Draw()
    {
        if (patterns == null || patterns.Length == 0) return null;

        float total = 0f;
        foreach (ObstaclePatternData p in patterns)
            total += p.weight;

        if (total <= 0f) return null;

        float roll = UnityEngine.Random.value * total;

        foreach (ObstaclePatternData p in patterns)
        {
            roll -= p.weight;
            if (roll <= 0f) return p;
        }

        // Floating-point safety — return last pattern.
        return patterns[patterns.Length - 1];
    }
}

/// <summary>
/// Inline pattern definition. Serialized directly inside <see cref="ObstaclePatternLibrarySO"/>.
/// </summary>
[Serializable]
public class ObstaclePatternData
{
    [Tooltip("Display name — purely descriptive.")]
    public string patternName = "New Pattern";

    [Tooltip("Relative weight in the random draw. Higher = more frequent.")]
    [Min(0f)]
    public float weight = 1f;

    [Tooltip("Ordered list of spawns that make up this pattern.")]
    public PatternStep[] steps = Array.Empty<PatternStep>();
}

/// <summary>One spawn event inside a pattern.</summary>
[Serializable]
public struct PatternStep
{
    [Tooltip("Row(s) to spawn at this step.")]
    public ObstacleRow row;

    [Tooltip("Distance (world units) the background must scroll AFTER this step before " +
             "the next step executes. The actual wait time is recomputed each frame " +
             "from the current scroll speed so visual spacing stays constant at any difficulty.\n\n" +
             "Rule of thumb: screen width ≈ 20 units. A value of 10 = half a screen gap.")]
    [Min(0f)]
    public float scrollDistanceAfter;
}

/// <summary>Which lane(s) to occupy at a given step.</summary>
public enum ObstacleRow
{
    Center,
    Top,
    Bottom,
    TopAndBottom
}
