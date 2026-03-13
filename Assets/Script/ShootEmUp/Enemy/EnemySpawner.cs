using UnityEngine;

/// <summary>
/// Phase-based enemy spawner.
///
/// Normal phase: spawns enemies from a weighted pool at a fixed interval,
/// respecting maxAliveAtOnce and maxSpawnsPerPhase caps.
/// Advances when phaseDuration expires OR when maxSpawnsPerPhase is reached and all enemies are dead.
///
/// Boss phase: spawns a single boss immediately at phase start.
/// All regular spawning is suspended. Advances only when the boss dies.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private SpawnerConfigSO config;

    private int _currentPhaseIndex;
    private float _phaseElapsed;
    private float _spawnTimer;
    private int _spawnedInPhase;
    private int _aliveEnemyCount;
    private bool _waitingForBossDeath;

    private PhaseDataSO CurrentPhase => config.phases[_currentPhaseIndex];

    private void Start()
    {
        if (config == null || config.phases == null || config.phases.Length == 0)
        {
            Debug.LogError("EnemySpawner: SpawnerConfigSO is missing or has no phases.");
            enabled = false;
            return;
        }

        BeginPhase();
    }

    private void OnEnable()  => GameOverEvents.OnGameOver += HandleGameOver;
    private void OnDisable() => GameOverEvents.OnGameOver -= HandleGameOver;

    private void HandleGameOver() => enabled = false;

    private void Update()
    {
        // During a boss phase the spawner is fully suspended until the boss dies.
        if (_waitingForBossDeath) return;

        _phaseElapsed += Time.deltaTime;
        _spawnTimer   += Time.deltaTime;

        TrySpawn();
        TryAdvanceNormalPhase();
    }

    // ── Phase lifecycle ────────────────────────────────────────────────────────

    private void BeginPhase()
    {
        _phaseElapsed    = 0f;
        _spawnTimer      = 0f;
        _spawnedInPhase  = 0;

        Debug.Log($"EnemySpawner: Phase {_currentPhaseIndex + 1} started ({CurrentPhase.phaseType}).");

        if (CurrentPhase.phaseType == PhaseType.Boss)
        {
            _waitingForBossDeath = true;
            SpawnBoss();
        }
    }

    private void AdvancePhase()
    {
        if (_currentPhaseIndex >= config.phases.Length - 1)
        {
            Debug.Log("EnemySpawner: All phases complete.");
            enabled = false;
            return;
        }

        _currentPhaseIndex++;
        BeginPhase();
    }

    // ── Boss phase ─────────────────────────────────────────────────────────────

    /// <summary>Instantiates the boss and subscribes to its death event.</summary>
    private void SpawnBoss()
    {
        if (CurrentPhase.bossPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: Boss phase has no bossPrefab assigned — skipping phase.");
            _waitingForBossDeath = false;
            AdvancePhase();
            return;
        }

        float y = Random.Range(config.minSpawnY, config.maxSpawnY);
        var boss = Instantiate(
            CurrentPhase.bossPrefab,
            new Vector3(config.spawnX, y, 0f),
            Quaternion.identity);

        var core = boss.GetComponent<EnemyCore>();
        if (core != null)
            core.OnDeathEvent += OnBossDied;
        else
            Debug.LogWarning("EnemySpawner: Boss prefab has no EnemyCore — phase will never advance.");
    }

    private void OnBossDied()
    {
        _waitingForBossDeath = false;
        Debug.Log("EnemySpawner: Boss killed — advancing to next phase.");
        AdvancePhase();
    }

    // ── Normal phase ───────────────────────────────────────────────────────────

    private void TrySpawn()
    {
        if (_spawnTimer < CurrentPhase.spawnInterval) return;

        // Respect maxAliveAtOnce cap.
        if (CurrentPhase.maxAliveAtOnce > 0 && _aliveEnemyCount >= CurrentPhase.maxAliveAtOnce) return;

        // Respect total spawn cap.
        if (CurrentPhase.maxSpawnsPerPhase > 0 && _spawnedInPhase >= CurrentPhase.maxSpawnsPerPhase) return;

        _spawnTimer = 0f;

        GameObject prefab = GetWeightedRandom(CurrentPhase.spawnPool);
        if (prefab == null) return;

        float y = Random.Range(config.minSpawnY, config.maxSpawnY);
        var enemy = Instantiate(prefab, new Vector3(config.spawnX, y, 0f), Quaternion.identity);

        _spawnedInPhase++;
        _aliveEnemyCount++;

        var core = enemy.GetComponent<EnemyCore>();
        if (core != null)
            core.OnDeathEvent += OnEnemyDied;
    }

    private void OnEnemyDied()
    {
        _aliveEnemyCount = Mathf.Max(0, _aliveEnemyCount - 1);
    }

    /// <summary>
    /// Advances the phase when either the timer expires or the spawn cap is
    /// exhausted and all spawned enemies are dead.
    /// </summary>
    private void TryAdvanceNormalPhase()
    {
        if (_currentPhaseIndex >= config.phases.Length - 1) return;

        bool timerDone = CurrentPhase.phaseDuration > 0f
            && _phaseElapsed >= CurrentPhase.phaseDuration;

        bool capReached = CurrentPhase.maxSpawnsPerPhase > 0
            && _spawnedInPhase >= CurrentPhase.maxSpawnsPerPhase
            && _aliveEnemyCount == 0;

        if (timerDone || capReached)
            AdvancePhase();
    }

    // ── Weighted random ────────────────────────────────────────────────────────

    /// <summary>Picks an enemy prefab from the pool using weight-based probability.</summary>
    private GameObject GetWeightedRandom(EnemySpawnEntry[] pool)
    {
        if (pool == null || pool.Length == 0) return null;

        float total = 0f;
        foreach (var entry in pool) total += entry.spawnWeight;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var entry in pool)
        {
            cumulative += entry.spawnWeight;
            if (roll <= cumulative) return entry.enemyPrefab;
        }

        return pool[pool.Length - 1].enemyPrefab;
    }
}
