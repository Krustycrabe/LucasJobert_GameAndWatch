using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Spawns obstacles from the right of the screen according to the current difficulty.
/// Four obstacle types are supported:
///   Center    — single obstacle at the centre lane (player must split)
///   Top       — single obstacle at top wall (player must stay merged or split-bottom only)
///   Bottom    — single obstacle at bottom wall
///   TopBottom — two obstacles (top + bottom); player must stay merged at centre
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private VirusSplitConfigSO config;

    [Header("Prefabs")]
    [Tooltip("Obstacle for the centre lane (globule blanc / brain bullet reuse).")]
    [SerializeField] private GameObject centerObstaclePrefab;
    [Tooltip("Obstacle for wall attachment (plaque immunitaire).")]
    [SerializeField] private GameObject wallObstaclePrefab;

    [Header("Obstacle Z-depth")]
    [SerializeField] private float obstacleZ = 0f;

    private Func<Vector2[]> _getVirusPositions;
    private float  _currentSpeed;
    private float  _spawnTimer;
    private float  _currentInterval;
    private float  _elapsedTime;
    private bool   _running;
    private bool   _gameOver;

    private void OnEnable()  => GameOverEvents.OnGameOver += HandleGameOver;
    private void OnDisable() => GameOverEvents.OnGameOver -= HandleGameOver;

    private void HandleGameOver() => _gameOver = true;

    /// <summary>Called by VirusController once the game starts.</summary>
    public void Initialize(VirusSplitConfigSO cfg, Func<Vector2[]> getVirusPositions)
    {
        config               = cfg;
        _getVirusPositions   = getVirusPositions;
        _currentInterval     = config.initialSpawnInterval;
        _spawnTimer          = _currentInterval;
        _running             = true;
    }

    /// <summary>Updated each frame by VirusController with the current scroll speed.</summary>
    public void UpdateSpeed(float speed) => _currentSpeed = speed;

    private void Update()
    {
        if (!_running || _gameOver) return;

        _elapsedTime += Time.deltaTime;

        // Decrease spawn interval over time (difficulty ramp), clamp to minimum.
        _currentInterval = Mathf.Max(
            config.minSpawnInterval,
            config.initialSpawnInterval - _elapsedTime * config.spawnIntervalDecreaseRate);

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer = _currentInterval;
            SpawnPattern(PickPattern());
        }
    }

    private ObstaclePattern PickPattern()
    {
        // Weight distribution: as difficulty grows, centre and TopBottom appear more often.
        float t = Mathf.Clamp01(_elapsedTime / 60f); // saturates at 60 s
        float rollCenter    = Mathf.Lerp(0.30f, 0.38f, t);
        float rollTop       = Mathf.Lerp(0.25f, 0.20f, t);
        float rollBottom    = Mathf.Lerp(0.25f, 0.20f, t);
        // rollTopBottom = remainder

        float roll = Random.value;
        if (roll < rollCenter)            return ObstaclePattern.Center;
        if (roll < rollCenter + rollTop)  return ObstaclePattern.Top;
        if (roll < rollCenter + rollTop + rollBottom) return ObstaclePattern.Bottom;
        return ObstaclePattern.TopBottom;
    }

    private void SpawnPattern(ObstaclePattern pattern)
    {
        switch (pattern)
        {
            case ObstaclePattern.Center:
                SpawnAt(centerObstaclePrefab, config.centerObstacleY);
                break;
            case ObstaclePattern.Top:
                SpawnAt(wallObstaclePrefab, config.topObstacleY);
                break;
            case ObstaclePattern.Bottom:
                SpawnAt(wallObstaclePrefab, config.bottomObstacleY);
                break;
            case ObstaclePattern.TopBottom:
                SpawnAt(wallObstaclePrefab, config.topObstacleY);
                SpawnAt(wallObstaclePrefab, config.bottomObstacleY);
                break;
        }
    }

    private void SpawnAt(GameObject prefab, float y)
    {
        if (prefab == null) return;
        Vector3 spawnPos = new Vector3(config.obstacleSpawnX, y, obstacleZ);
        GameObject go    = Instantiate(prefab, spawnPos, Quaternion.identity, transform);

        ObstacleMover mover = go.GetComponent<ObstacleMover>();
        if (mover == null) mover = go.AddComponent<ObstacleMover>();

        mover.Initialize(config, _getVirusPositions);
        mover.SetSpeed(_currentSpeed);
    }
}

public enum ObstaclePattern { Center, Top, Bottom, TopBottom }
