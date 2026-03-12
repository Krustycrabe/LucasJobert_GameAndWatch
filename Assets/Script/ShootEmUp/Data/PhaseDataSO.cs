using System;
using UnityEngine;

public enum PhaseType { Normal, Boss }

/// <summary>
/// Defines a single phase of the game.
/// Normal: spawns enemies from a weighted pool with interval and cap controls.
/// Boss: spawns one boss immediately; the phase ends only when the boss is killed.
/// </summary>
[CreateAssetMenu(fileName = "PhaseData", menuName = "ShootEmUp/Spawner Phase")]
public class PhaseDataSO : ScriptableObject
{
    [Header("Phase Type")]
    [Tooltip("Normal = timed pool spawning. Boss = single boss spawn, phase ends on boss death.")]
    public PhaseType phaseType = PhaseType.Normal;

    // ── Normal phase ───────────────────────────────────────────────────────────

    [Header("Normal — Timing")]
    [Tooltip("Phase duration in seconds before advancing. Set to 0 to use maxSpawnsPerPhase as the only end condition.")]
    public float phaseDuration = 30f;

    [Tooltip("Seconds between each enemy spawn.")]
    public float spawnInterval = 3f;

    [Header("Normal — Caps")]
    [Tooltip("Maximum total enemies spawned during this phase. 0 = unlimited.")]
    public int maxSpawnsPerPhase = 0;

    [Tooltip("Maximum enemies alive at the same time. Spawn is paused when this cap is reached. 0 = unlimited.")]
    public int maxAliveAtOnce = 0;

    [Header("Normal — Pool")]
    [Tooltip("Weighted pool of enemies available in this phase.")]
    public EnemySpawnEntry[] spawnPool;

    // ── Boss phase ─────────────────────────────────────────────────────────────

    [Header("Boss")]
    [Tooltip("Boss prefab spawned automatically at the start of this phase.")]
    public GameObject bossPrefab;
}

[Serializable]
public class EnemySpawnEntry
{
    public GameObject enemyPrefab;

    [Tooltip("Higher weight = more likely to be chosen during weighted random selection.")]
    public float spawnWeight = 1f;
}
