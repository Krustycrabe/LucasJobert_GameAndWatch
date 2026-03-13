using UnityEngine;

/// <summary>
/// Moves an obstacle from right to left at the current scroll speed.
/// Destroys itself when it exits the left boundary defined in VirusSplitConfigSO.
/// Triggers game over on collision with any collider tagged "Player".
/// Near-miss detection: when the obstacle passes the virus X position, if a virus was
/// within nearMissYDistance without colliding, triggers SlowMotionManager.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ObstacleMover : MonoBehaviour
{
    private VirusSplitConfigSO _config;
    private float    _speed;
    private bool     _gameOver;
    private bool     _nearMissChecked;

    // References to active virus positions for near-miss detection.
    // Set via Initialize(); the array can have 1 or 2 entries depending on virus state.
    private System.Func<Vector2[]> _getVirusPositions;

    /// <summary>
    /// Called by ObstacleSpawner after instantiation.
    /// getVirusPositions: a delegate that returns the current world positions of active viruses.
    /// </summary>
    public void Initialize(VirusSplitConfigSO config, System.Func<Vector2[]> getVirusPositions)
    {
        _config           = config;
        _getVirusPositions = getVirusPositions;
    }

    public void SetSpeed(float speed) => _speed = speed;

    private void OnEnable()  => GameOverEvents.OnGameOver += HandleGameOver;
    private void OnDisable() => GameOverEvents.OnGameOver -= HandleGameOver;

    private void HandleGameOver() => _gameOver = true;

    private void Update()
    {
        if (_gameOver) return;

        transform.position += Vector3.left * _speed * Time.deltaTime;

        if (transform.position.x < _config.obstacleDestroyX)
        {
            Destroy(gameObject);
            return;
        }

        CheckNearMiss();
    }

    private void CheckNearMiss()
    {
        if (_nearMissChecked || _getVirusPositions == null) return;

        Vector2[] positions = _getVirusPositions.Invoke();
        if (positions == null || positions.Length == 0) return;

        float obstX = transform.position.x;

        foreach (Vector2 virusPos in positions)
        {
            // Evaluate only once as the obstacle passes the virus' X.
            if (obstX > virusPos.x - _config.nearMissXWindow) continue;

            _nearMissChecked = true;

            float yDist = Mathf.Abs(transform.position.y - virusPos.y);
            if (yDist < _config.nearMissYDistance)
                SlowMotionManager.Instance?.TriggerSlowMotion();

            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        GameOverEvents.RaiseGameOver();
    }
}
