using UnityEngine;

/// <summary>
/// Central configuration ScriptableObject for the VirusSplit mini-game.
/// Create via right-click > VirusSplit > Config.
/// </summary>
[CreateAssetMenu(fileName = "VirusSplitConfig", menuName = "VirusSplit/Config")]
public class VirusSplitConfigSO : ScriptableObject
{
    [Header("Virus Split Positions")]
    [Tooltip("World Y of the top virus when split.")]
    public float splitTopY    =  2.4f;
    [Tooltip("World Y of the bottom virus when split.")]
    public float splitBottomY = -2.4f;
    [Tooltip("Seconds to complete the split transition.")]
    public float splitDuration  = 0.14f;
    [Tooltip("Seconds to complete the merge transition.")]
    public float mergeDuration  = 0.14f;
    [Tooltip("Easing curve for split/merge movement (X = normalized time, Y = normalized position).")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Scroll Speed")]
    [Tooltip("Initial scroll speed of obstacles and background (world units/s).")]
    public float initialScrollSpeed      = 5f;
    [Tooltip("Maximum scroll speed reached at max difficulty.")]
    public float maxScrollSpeed          = 14f;
    [Tooltip("Speed gained per second of play time.")]
    public float speedIncreaseRate       = 0.08f;

    [Header("Obstacles")]
    [Tooltip("Spawn X (right of screen, world space).")]
    public float obstacleSpawnX          = 11f;
    [Tooltip("Destroy X (left of screen, world space).")]
    public float obstacleDestroyX        = -11f;
    [Tooltip("Y of the centre lane obstacle (globule blanc).")]
    public float centerObstacleY         = 0f;
    [Tooltip("Y of the top wall obstacle.")]
    public float topObstacleY            = 2.9f;
    [Tooltip("Y of the bottom wall obstacle.")]
    public float bottomObstacleY         = -2.9f;
    [Tooltip("Initial seconds between obstacle spawns.")]
    public float initialSpawnInterval    = 2.8f;
    [Tooltip("Minimum seconds between obstacle spawns at max difficulty.")]
    public float minSpawnInterval        = 0.85f;
    [Tooltip("Spawn interval decrease per second of play time.")]
    public float spawnIntervalDecreaseRate = 0.04f;

    [Header("Near-Miss Slow Motion")]
    [Tooltip("Radius of the proximity trigger collider created on each obstacle (child GO).")]
    public float nearMissTriggerRadius  = 1.2f;
    [Tooltip("Time scale applied during the near-miss slow motion.")]
    [Range(0.05f, 0.9f)]
    public float slowMotionScale        = 0.22f;
    [Tooltip("Real-time duration of the slow-motion plateau (seconds).")]
    public float slowMotionDuration     = 0.5f;
    [Tooltip("Real-time duration to lerp back to normal time scale after the plateau.")]
    public float slowMotionRecovery     = 0.35f;

    [Header("Score")]
    [Tooltip("Metres awarded per world unit of scroll (score = distance * metersPerUnit).")]
    public float metersPerUnit           = 0.5f;
}
