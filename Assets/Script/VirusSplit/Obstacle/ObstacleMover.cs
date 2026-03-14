using UnityEngine;

/// <summary>
/// Moves an obstacle from right to left at the current scroll speed.
/// Triggers game over via GameOverEvents when a collider tagged "Player" enters.
///
/// On reaching the despawn X threshold the obstacle is returned to the
/// ObstacleSpawner pool (no Destroy = no GC spike).
///
/// Near-miss detection is handled by a dynamically-created child GameObject with
/// ObstacleNearMissTrigger + a larger CircleCollider2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ObstacleMover : MonoBehaviour
{
    private VirusSplitConfigSO  _config;
    private ObstacleSpawner     _pool;
    private float               _speed;
    private bool                _gameOver;
    private bool                _isCenter;
    private bool                _nearMissCreated;

    /// <summary>Called by ObstacleSpawner each time the obstacle is taken from the pool.</summary>
    public void Initialize(
        VirusSplitConfigSO    config,
        System.Func<Vector2[]> getVirusPositions,
        ObstacleSpawner        pool,
        bool                   isCenter)
    {
        _config   = config;
        _pool     = pool;
        _isCenter = isCenter;
        _gameOver = false;

        // Create the near-miss child only the very first time — it moves with the parent.
        if (!_nearMissCreated)
        {
            CreateNearMissTrigger();
            _nearMissCreated = true;
        }
    }

    /// <summary>Updated by ObstacleSpawner via SetSpeed().</summary>
    public void SetSpeed(float speed) => _speed = speed;

    private void OnEnable()  => GameOverEvents.OnGameOver += HandleGameOver;
    private void OnDisable() => GameOverEvents.OnGameOver -= HandleGameOver;

    private void HandleGameOver() => _gameOver = true;

    private void Start()
    {
        // Disable ShootEmUp movement & collision scripts that would interfere.
        var ebm = GetComponent<EnemyBulletMover>();
        if (ebm != null) ebm.enabled = false;

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        if (_gameOver) return;

        transform.position += Vector3.left * _speed * Time.deltaTime;

        if (_config != null && transform.position.x < _config.obstacleDestroyX)
            ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            GameOverEvents.RaiseGameOver();
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private void ReturnToPool()
    {
        if (_pool != null)
            _pool.ReturnToPool(gameObject, _isCenter);
        else
            gameObject.SetActive(false);
    }

    /// <summary>
    /// Creates a child GO with ObstacleNearMissTrigger and a CircleCollider2D sized
    /// to nearMissTriggerRadius. Only called once per pooled instance.
    /// </summary>
    private void CreateNearMissTrigger()
    {
        float radius = _config != null ? _config.nearMissTriggerRadius : 1.2f;

        var child = new GameObject("NearMissTrigger");
        child.transform.SetParent(transform, false);
        child.transform.localPosition = Vector3.zero;

        var circle       = child.AddComponent<CircleCollider2D>();
        circle.radius    = radius;
        circle.isTrigger = true;

        child.AddComponent<ObstacleNearMissTrigger>();
    }
}

