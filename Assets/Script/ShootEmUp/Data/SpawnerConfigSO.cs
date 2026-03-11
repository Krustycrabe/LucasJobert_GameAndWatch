using UnityEngine;

/// <summary>
/// Global configuration for the enemy spawner. Holds all phases and spawn position settings.
/// </summary>
[CreateAssetMenu(fileName = "SpawnerConfig", menuName = "ShootEmUp/Spawner Config")]
public class SpawnerConfigSO : ScriptableObject
{
    [Tooltip("Ordered list of phases. The game progresses through them over time.")]
    public PhaseDataSO[] phases;

    [Header("Spawn Position")]
    [Tooltip("X position where enemies spawn (right edge of screen).")]
    public float spawnX = 12f;

    [Tooltip("Y range for random spawn position.")]
    public float minSpawnY = -4f;
    public float maxSpawnY = 4f;
}
