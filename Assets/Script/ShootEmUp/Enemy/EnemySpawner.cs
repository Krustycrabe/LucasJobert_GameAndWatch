using UnityEngine;

/// <summary>
/// Continuous enemy spawner with phase-based progression driven by elapsed time.
/// Each phase defines a weighted pool and a global spawn interval.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private SpawnerConfigSO config;

    private int _currentPhaseIndex;
    private float _phaseElapsed;
    private float _spawnTimer;

    private PhaseDataSO CurrentPhase => config.phases[_currentPhaseIndex];

    private void Start()
    {
        if (config == null || config.phases.Length == 0)
        {
            Debug.LogError("EnemySpawner: SpawnerConfigSO is missing or has no phases.");
            enabled = false;
            return;
        }

        _currentPhaseIndex = 0;
    }

    private void Update()
    {
        _phaseElapsed += Time.deltaTime;
        _spawnTimer += Time.deltaTime;

        TryAdvancePhase();
        TrySpawn();
    }

    private void TryAdvancePhase()
    {
        if (_currentPhaseIndex >= config.phases.Length - 1) return;
        if (_phaseElapsed >= CurrentPhase.phaseDuration)
        {
            _currentPhaseIndex++;
            _phaseElapsed = 0f;
            _spawnTimer = 0f;
            Debug.Log($"EnemySpawner: Entering phase {_currentPhaseIndex + 1}");
        }
    }

    private void TrySpawn()
    {
        if (_spawnTimer < CurrentPhase.spawnInterval) return;
        _spawnTimer = 0f;

        GameObject prefab = GetWeightedRandom(CurrentPhase.spawnPool);
        if (prefab == null) return;

        float y = Random.Range(config.minSpawnY, config.maxSpawnY);
        Instantiate(prefab, new Vector3(config.spawnX, y, 0f), Quaternion.identity);
    }

    /// <summary>Picks a random enemy from the pool using weight-based probability.</summary>
    private GameObject GetWeightedRandom(EnemySpawnEntry[] pool)
    {
        if (pool == null || pool.Length == 0) return null;

        float totalWeight = 0f;
        foreach (var entry in pool) totalWeight += entry.spawnWeight;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var entry in pool)
        {
            cumulative += entry.spawnWeight;
            if (roll <= cumulative) return entry.enemyPrefab;
        }

        return pool[pool.Length - 1].enemyPrefab;
    }
}
