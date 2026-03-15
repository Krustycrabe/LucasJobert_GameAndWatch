using UnityEngine;

/// <summary>
/// Moves an obstacle right-to-left at the assigned speed.
/// Supports a "fast" mode (set by ObstacleSpawner) that applies a speed multiplier
/// and tints the SpriteRenderer to give the player a visual warning.
/// Returns itself to the ObstacleSpawner pool on despawn — no Destroy/GC.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ObstacleMover : MonoBehaviour
{
    [Tooltip("Tint applied to the sprite when this obstacle is a fast one.")]
    [SerializeField] private Color fastColor = new Color(1f, 0.45f, 0.1f, 1f);

    private VirusSplitConfigSO  _config;
    private ObstacleSpawner     _pool;
    private PoolEntry           _poolEntry;
    private float               _speed;
    private bool                _gameOver;
    private bool                _isCenter;
    private bool                _nearMissCreated;

    private SpriteRenderer _sprite;
    private Color          _defaultColor;

    /// <summary>Called by ObstacleSpawner each time the obstacle is taken from the pool.</summary>
    public void Initialize(
        VirusSplitConfigSO     config,
        System.Func<Vector2[]> getVirusPositions,
        ObstacleSpawner        pool,
        PoolEntry              poolEntry,
        bool                   isCenter)
    {
        _config    = config;
        _pool      = pool;
        _poolEntry = poolEntry;
        _isCenter  = isCenter;
        _gameOver  = false;

        if (!_nearMissCreated)
        {
            CreateNearMissTrigger();
            _nearMissCreated = true;
        }
    }

    /// <summary>Sets the movement speed for this obstacle.</summary>
    public void SetSpeed(float speed) => _speed = speed;

    /// <summary>
    /// Marks this obstacle as fast and applies a visual tint so the player
    /// can react ahead of time. Pass false to restore the default appearance.
    /// </summary>
    public void SetFast(bool fast)
    {
        if (_sprite == null) return;
        _sprite.color = fast ? fastColor : _defaultColor;
    }

    private void OnEnable()  => GameOverEvents.OnGameOver += HandleGameOver;
    private void OnDisable() => GameOverEvents.OnGameOver -= HandleGameOver;
    private void HandleGameOver() => _gameOver = true;

    private void Start()
    {
        var ebm = GetComponent<EnemyBulletMover>();
        if (ebm != null) ebm.enabled = false;

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        _sprite       = GetComponentInChildren<SpriteRenderer>();
        _defaultColor = _sprite != null ? _sprite.color : Color.white;
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

    // ── Private ───────────────────────────────────────────────────────────────

    private void ReturnToPool()
    {
        if (_sprite != null) _sprite.color = _defaultColor;

        if (_pool != null)
            _pool.ReturnToPool(_poolEntry, _isCenter);
        else
            gameObject.SetActive(false);
    }

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

