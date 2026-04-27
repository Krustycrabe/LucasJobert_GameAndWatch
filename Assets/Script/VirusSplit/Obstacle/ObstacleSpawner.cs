using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns obstacles according to patterns defined in <see cref="ObstaclePatternLibrarySO"/>.
///
/// Flow:
///   1. PrewarmCoroutine() allocates pool instances one per frame (no spike on start).
///   2. SpawnLoop() runs indefinitely: draws a pattern from the library, then executes
///      each step in order, waiting step.delayAfter seconds between steps.
///   3. Speed and fast-obstacle chance ramp up with elapsed time exactly as before.
///
/// The pool, ObstacleMover API, and VirusSplitConfigSO are unchanged so no other
/// script needs to be modified.
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private VirusSplitConfigSO config;

    [Header("Pattern Library")]
    [Tooltip("Library containing all patterns available for random selection.")]
    [SerializeField] private ObstaclePatternLibrarySO patternLibrary;

    [Header("Prefabs")]
    [Tooltip("Centre-lane obstacle.")]
    [SerializeField] private GameObject centerObstaclePrefab;
    [Tooltip("Wall obstacle (top and bottom).")]
    [SerializeField] private GameObject wallObstaclePrefab;

    [Header("Pooling")]
    [Tooltip("Number of instances pre-created per prefab at init time.")]
    [SerializeField] private int poolPrewarm = 8;

    [Header("Z depth")]
    [SerializeField] private float obstacleZ = 0f;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private Func<Vector2[]> _getVirusPositions;
    private float           _currentSpeed;
    private float           _elapsedTime;
    private bool            _running;
    private bool            _gameOver;
    private Coroutine       _spawnLoop;

    // ── Pools ─────────────────────────────────────────────────────────────────
    private readonly Queue<PoolEntry> _centerPool = new Queue<PoolEntry>();
    private readonly Queue<PoolEntry> _wallPool   = new Queue<PoolEntry>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()  => GameOverEvents.OnGameOver += HandleGameOver;
    private void OnDisable() => GameOverEvents.OnGameOver -= HandleGameOver;

    private void HandleGameOver()
    {
        _gameOver = true;
        if (_spawnLoop != null)
        {
            StopCoroutine(_spawnLoop);
            _spawnLoop = null;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Called by VirusController at game start.</summary>
    public void Initialize(VirusSplitConfigSO cfg, Func<Vector2[]> getVirusPositions)
    {
        config             = cfg;
        _getVirusPositions = getVirusPositions;
        StartCoroutine(PrewarmCoroutine());
    }

    /// <summary>Updated each frame by VirusController.</summary>
    public void UpdateSpeed(float speed) => _currentSpeed = speed;

    /// <summary>
    /// Returns an obstacle to its pool. Called by ObstacleMover when the
    /// obstacle reaches the despawn X threshold.
    /// </summary>
    public void ReturnToPool(PoolEntry entry, bool isCenter)
    {
        entry.Go.SetActive(false);
        (isCenter ? _centerPool : _wallPool).Enqueue(entry);
    }

    // ── Elapsed time ──────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_running || _gameOver) return;
        _elapsedTime += Time.deltaTime;
    }

    // ── Spawn loop ────────────────────────────────────────────────────────────

    /// <summary>
    /// Main spawn coroutine. Draws one pattern, executes its steps in order,
    /// then waits for the background to scroll <c>scrollDistanceAfter</c> world units
    /// before the next step — keeping visual spacing constant at any speed.
    /// </summary>
    private IEnumerator SpawnLoop()
    {
        while (!_gameOver)
        {
            ObstaclePatternData pattern = patternLibrary != null ? patternLibrary.Draw() : null;

            if (pattern == null || pattern.steps == null || pattern.steps.Length == 0)
            {
                Debug.LogWarning("[ObstacleSpawner] No valid pattern found in library.");
                yield return new WaitForSeconds(1f);
                continue;
            }

            foreach (PatternStep step in pattern.steps)
            {
                if (_gameOver) yield break;

                ExecuteStep(step.row);

                if (step.scrollDistanceAfter > 0f)
                    yield return ScrollDistanceWait(step.scrollDistanceAfter);
            }
        }
    }

    /// <summary>
    /// Yields until the background has scrolled <paramref name="worldUnits"/> units
    /// at the current (live) speed. Recalculates each frame so speed changes mid-wait
    /// are fully accounted for.
    /// </summary>
    private IEnumerator ScrollDistanceWait(float worldUnits)
    {
        float remaining = worldUnits;
        while (remaining > 0f && !_gameOver)
        {
            remaining -= _currentSpeed * Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>Spawns obstacle(s) for the given row.</summary>
    private void ExecuteStep(ObstacleRow row)
    {
        switch (row)
        {
            case ObstacleRow.Center:
                SpawnAt(centerObstaclePrefab, _centerPool, config.centerObstacleY, isCenter: true);
                break;

            case ObstacleRow.Top:
                SpawnAt(wallObstaclePrefab, _wallPool, config.topObstacleY, isCenter: false);
                break;

            case ObstacleRow.Bottom:
                SpawnAt(wallObstaclePrefab, _wallPool, config.bottomObstacleY, isCenter: false);
                break;

            case ObstacleRow.TopAndBottom:
                SpawnAt(wallObstaclePrefab, _wallPool, config.topObstacleY,    isCenter: false);
                SpawnAt(wallObstaclePrefab, _wallPool, config.bottomObstacleY, isCenter: false);
                break;
        }
    }

    // ── SpawnAt ───────────────────────────────────────────────────────────────

    private void SpawnAt(GameObject prefab, Queue<PoolEntry> pool, float y, bool isCenter)
    {
        if (prefab == null) return;

        PoolEntry entry = pool.Count > 0 ? pool.Dequeue() : CreateInstance(prefab);

        entry.Go.transform.position = new Vector3(config.obstacleSpawnX, y, obstacleZ);
        entry.Go.SetActive(true);

        entry.Mover.Initialize(config, _getVirusPositions, this, entry, isCenter);

        // Fast obstacle ramp — unchanged from original.
        float t      = Mathf.Clamp01(_elapsedTime / config.fastObstacleRampDuration);
        float chance = Mathf.Lerp(config.fastObstacleChanceStart, config.fastObstacleChanceMax, t);
        bool  isFast = UnityEngine.Random.value < chance;
        float speed  = isFast ? _currentSpeed * config.fastObstacleSpeedMultiplier : _currentSpeed;

        entry.Mover.SetSpeed(speed);
        entry.Mover.SetFast(isFast);
    }

    // ── Pool helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Instantiates pool instances one per frame to avoid a Physics2D spike on
    /// the first frame. _running and the spawn loop start only after prewarm completes.
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

        _running   = true;
        _spawnLoop = StartCoroutine(SpawnLoop());
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

/// <summary>
/// Pool entry that caches the ObstacleMover reference to avoid
/// a GetComponent call on every spawn.
/// </summary>
public class PoolEntry
{
    public GameObject    Go;
    public ObstacleMover Mover;
}
