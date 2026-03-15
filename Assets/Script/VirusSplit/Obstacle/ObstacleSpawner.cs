using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Spawns obstacles from the right of the screen using per-prefab object pools
/// to eliminate per-spawn Instantiate/Destroy GC pressure and frame spikes.
///
/// Four obstacle patterns:
///   Center    — single obstacle at the centre lane (player must split)
///   Top       — single obstacle at top wall
///   Bottom    — single obstacle at bottom wall
///   TopBottom — two obstacles (top + bottom); player must stay merged at centre
///
/// Obstacles are returned to the pool via ObstacleMover.OnReturnToPool().
/// The pool pre-warms <poolPrewarm> instances per prefab at Initialize() time
/// so no allocation occurs during gameplay.
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private VirusSplitConfigSO config;

    [Header("Prefabs")]
    [Tooltip("Centre-lane obstacle.")]
    [SerializeField] private GameObject centerObstaclePrefab;
    [Tooltip("Wall obstacle.")]
    [SerializeField] private GameObject wallObstaclePrefab;

    [Header("Pooling")]
    [Tooltip("Number of instances pre-created per prefab at init time.")]
    [SerializeField] private int poolPrewarm = 8;

    [Header("Randomisation")]
    [Range(0f, 0.5f)]
    [SerializeField] private float jitterFraction    = 0.35f;
    [SerializeField] private float topBottomCooldown = 3.5f;

    [Header("Z depth")]
    [SerializeField] private float obstacleZ = 0f;

    private Func<Vector2[]> _getVirusPositions;
    private float           _currentSpeed;
    private float           _elapsedTime;
    private float           _spawnTimer;
    private float           _lastTopBottomTime = -999f;
    private bool            _running;
    private bool            _gameOver;

    // ── Pools ─────────────────────────────────────────────────────────────────
    private readonly Queue<PoolEntry> _centerPool = new Queue<PoolEntry>();
    private readonly Queue<PoolEntry> _wallPool   = new Queue<PoolEntry>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()  => GameOverEvents.OnGameOver += HandleGameOver;
    private void OnDisable() => GameOverEvents.OnGameOver -= HandleGameOver;
    private void HandleGameOver() => _gameOver = true;

    /// <summary>Called by VirusController at game start.</summary>
    public void Initialize(VirusSplitConfigSO cfg, Func<Vector2[]> getVirusPositions)
    {
        config             = cfg;
        _getVirusPositions = getVirusPositions;
        _spawnTimer        = ComputeNextInterval();

        // Prewarm is spread across multiple frames (one instance per frame) so the
        // first frame after scene load does not spike with 16 simultaneous Instantiate
        // calls + Physics2D broadphase rebuilds, which was causing the input freeze.
        StartCoroutine(PrewarmCoroutine());
    }

    /// <summary>Updated each frame by VirusController.</summary>
    public void UpdateSpeed(float speed) => _currentSpeed = speed;

    private void Update()
    {
        if (!_running || _gameOver) return;

        _elapsedTime += Time.deltaTime;
        _spawnTimer  -= Time.deltaTime;

        if (_spawnTimer <= 0f)
        {
            SpawnPattern(PickPattern());
            _spawnTimer = ComputeNextInterval();
        }
    }

    // ── Pool ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns an obstacle to its pool. Called by ObstacleMover when the
    /// obstacle reaches the despawn X threshold.
    /// </summary>
    public void ReturnToPool(PoolEntry entry, bool isCenter)
    {
        entry.Go.SetActive(false);
        (isCenter ? _centerPool : _wallPool).Enqueue(entry);
    }    // ── Interval ──────────────────────────────────────────────────────────────

    private float ComputeNextInterval()
    {
        float baseInterval = Mathf.Max(
            config.minSpawnInterval,
            config.initialSpawnInterval - _elapsedTime * config.spawnIntervalDecreaseRate);

        float jitter = Random.Range(1f - jitterFraction, 1f + jitterFraction);
        return Mathf.Max(config.minSpawnInterval * 0.65f, baseInterval * jitter);
    }

    // ── Pattern selection ─────────────────────────────────────────────────────

    private ObstaclePattern PickPattern()
    {
        float t = Mathf.Clamp01(_elapsedTime / 90f);

        float wCenter    = Mathf.Lerp(0.30f, 0.35f, t);
        float wTop       = Mathf.Lerp(0.28f, 0.22f, t);
        float wBottom    = Mathf.Lerp(0.28f, 0.22f, t);
        float wTopBottom = Mathf.Lerp(0.14f, 0.21f, t);

        if ((_elapsedTime - _lastTopBottomTime) < topBottomCooldown)
            wTopBottom = 0f;

        float total = wCenter + wTop + wBottom + wTopBottom;
        float roll  = Random.value * total;

        if (roll < wCenter)   return ObstaclePattern.Center;
        roll -= wCenter;
        if (roll < wTop)      return ObstaclePattern.Top;
        roll -= wTop;
        if (roll < wBottom)   return ObstaclePattern.Bottom;
        return ObstaclePattern.TopBottom;
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    private void SpawnPattern(ObstaclePattern pattern)
    {
        if (pattern == ObstaclePattern.TopBottom)
            _lastTopBottomTime = _elapsedTime;

        switch (pattern)
        {
            case ObstaclePattern.Center:
                SpawnAt(centerObstaclePrefab, _centerPool, config.centerObstacleY, isCenter: true);
                break;
            case ObstaclePattern.Top:
                SpawnAt(wallObstaclePrefab, _wallPool, config.topObstacleY, isCenter: false);
                break;
            case ObstaclePattern.Bottom:
                SpawnAt(wallObstaclePrefab, _wallPool, config.bottomObstacleY, isCenter: false);
                break;
            case ObstaclePattern.TopBottom:
                SpawnAt(wallObstaclePrefab, _wallPool, config.topObstacleY,    isCenter: false);
                SpawnAt(wallObstaclePrefab, _wallPool, config.bottomObstacleY, isCenter: false);
                break;
        }
    }

    private void SpawnAt(GameObject prefab, Queue<PoolEntry> pool, float y, bool isCenter)
    {
        if (prefab == null) return;

        PoolEntry entry = pool.Count > 0 ? pool.Dequeue() : CreateInstance(prefab);

        entry.Go.transform.position = new Vector3(config.obstacleSpawnX, y, obstacleZ);
        entry.Go.SetActive(true);

        // ObstacleMover is cached in the PoolEntry — no GetComponent per spawn.
        entry.Mover.Initialize(config, _getVirusPositions, this, entry, isCenter);

        float t      = Mathf.Clamp01(_elapsedTime / config.fastObstacleRampDuration);
        float chance = Mathf.Lerp(config.fastObstacleChanceStart, config.fastObstacleChanceMax, t);
        bool  isFast = Random.value < chance;
        float speed  = isFast ? _currentSpeed * config.fastObstacleSpeedMultiplier : _currentSpeed;
        entry.Mover.SetSpeed(speed);
        entry.Mover.SetFast(isFast);
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Instantiates pool instances one per frame to avoid a CPU/Physics2D spike
    /// on the first frame. _running is set to true only after all instances exist
    /// so no spawn can occur against an incomplete pool.
    /// </summary>
    private IEnumerator PrewarmCoroutine()
    {
        for (int i = 0; i < poolPrewarm; i++)
        {
            if (centerObstaclePrefab != null)
                _centerPool.Enqueue(CreateInstance(centerObstaclePrefab));
            if (wallObstaclePrefab != null)
                _wallPool.Enqueue(CreateInstance(wallObstaclePrefab));
            yield return null;
        }

        _running = true;
    }

    private PoolEntry CreateInstance(GameObject prefab)
    {
        GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
        go.SetActive(false);

        var ebm = go.GetComponent<EnemyBulletMover>();
        if (ebm != null) ebm.enabled = false;

        var mover = go.GetComponent<ObstacleMover>();
        if (mover == null) mover = go.AddComponent<ObstacleMover>();

        return new PoolEntry { Go = go, Mover = mover };
    }
}

public enum ObstaclePattern { Center, Top, Bottom, TopBottom }

/// <summary>
/// Pool entry that caches the ObstacleMover reference to avoid
/// a GetComponent call on every spawn.
/// </summary>
public class PoolEntry
{
    public GameObject    Go;
    public ObstacleMover Mover;
}
