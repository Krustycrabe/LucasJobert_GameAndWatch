using UnityEngine;

/// <summary>
/// ShootEmUp bridge toward the neutral GameOverEvents channel.
/// Connects PlayerHealth.OnDead and SmUpScoreManager.OnScoreChanged
/// so GameOverScreen reacts without any knowledge of ShootEmUp internals.
/// Also stops CameraShake when the game ends.
/// </summary>
public class GameOverBridge : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    private SmUpScoreManager _scoreManager;
    private bool _gameOverRaised;

    private void Awake()
    {
        // Subscribe as early as possible — Start may be too late if PlayerHealth
        // dies during the first frame (edge case with very low maxLives in testing).
        if (playerHealth != null)
            playerHealth.OnDead += HandlePlayerDead;
        else
            Debug.LogError("[GameOverBridge] PlayerHealth reference is missing. Game Over will not trigger.", this);
    }

    private void Start()
    {
        _scoreManager = SmUpScoreManager.Instance;

        if (_scoreManager != null)
            _scoreManager.OnScoreChanged += GameOverEvents.RaiseScoreUpdated;
        else
            Debug.LogWarning("[GameOverBridge] SmUpScoreManager instance not found.", this);
    }

    private void OnEnable()  => GameOverEvents.OnGameOver += HandleGameOverEffects;
    private void OnDisable() => GameOverEvents.OnGameOver -= HandleGameOverEffects;

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnDead -= HandlePlayerDead;

        if (_scoreManager != null)
            _scoreManager.OnScoreChanged -= GameOverEvents.RaiseScoreUpdated;
    }

    private void HandlePlayerDead()
    {
        if (_gameOverRaised) return;
        _gameOverRaised = true;
        GameOverEvents.RaiseGameOver();
    }

    /// <summary>Stops all continuous camera shake when the game ends.</summary>
    private void HandleGameOverEffects() => CameraShake.Instance?.StopContinuousShake();
}
