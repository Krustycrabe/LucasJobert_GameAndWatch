using UnityEngine;

/// <summary>
/// Global configuration for the enemy spawner. Holds all phases and spawn position settings.
/// </summary>
[CreateAssetMenu(fileName = "SpawnerConfig", menuName = "ShootEmUp/Spawner Config")]
public class SpawnerConfigSO : ScriptableObject
{
    [Tooltip("Ordered list of phases. The game progresses through them in order.")]
    public PhaseDataSO[] phases;

    [Header("Spawn Position")]
    [Tooltip("X position where enemies spawn (right edge of screen).")]
    public float spawnX = 12f;

    [Tooltip("Y range for random spawn position.")]
    public float minSpawnY = -4f;
    public float maxSpawnY = 4f;

    [Header("Infinite Loop Scaling")]
    [Tooltip("If true, phases restart from the beginning after the last phase ends, with increasing difficulty.")]
    public bool loopInfinitely = true;

    [Tooltip("Multiplier applied to enemy RuntimeSpeed each loop. > 1 = faster movement.")]
    public float moveSpeedMultiplier = 1.15f;

    [Tooltip("Multiplier applied to enemy RuntimeShootInterval each loop. < 1 = faster shooting.")]
    public float shootIntervalMultiplier = 0.9f;

    [Tooltip("Multiplier applied to the phase spawn interval each loop. < 1 = enemies spawn more often.")]
    public float spawnIntervalMultiplier = 0.85f;

    [Tooltip("Amount added to maxAliveAtOnce each loop (when the phase cap is > 0).")]
    public int maxAliveIncrement = 1;
}
