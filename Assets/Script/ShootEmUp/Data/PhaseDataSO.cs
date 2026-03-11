using System;
using UnityEngine;

/// <summary>
/// Defines a single difficulty phase: spawn frequency and available enemy pool.
/// </summary>
[CreateAssetMenu(fileName = "PhaseData", menuName = "ShootEmUp/Spawner Phase")]
public class PhaseDataSO : ScriptableObject
{
    [Tooltip("Duration of this phase in seconds before advancing to the next.")]
    public float phaseDuration = 30f;

    [Tooltip("How often (in seconds) an enemy spawns during this phase.")]
    public float spawnInterval = 3f;

    [Tooltip("Weighted pool of enemies available in this phase.")]
    public EnemySpawnEntry[] spawnPool;
}

[Serializable]
public class EnemySpawnEntry
{
    public GameObject enemyPrefab;

    [Tooltip("Higher weight = more likely to be chosen during weighted random selection.")]
    public float spawnWeight = 1f;
}
