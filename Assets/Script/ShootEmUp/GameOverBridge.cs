using UnityEngine;

/// <summary>
/// ShootEmUp bridge toward the neutral GameOverEvents channel.
/// Connects PlayerHealth.OnDead and SmUpScoreManager.OnScoreChanged
/// so GameOverScreen reacts without any knowledge of ShootEmUp internals.
/// Also stops CameraShake when the game ends.
/// To support a new level, create an equivalent bridge for that level's systems.
/// </summary>
public class GameOverBridge : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    private SmUpScoreManager _scoreManager;

    private void Start()
    {
        _scoreManager = SmUpScoreManager.Instance;

        if (playerHealth != null)
            playerHealth.OnDead += HandlePlayerDead;
        else
            Debug.LogWarning("[GameOverBridge] PlayerHealth reference is missing.", this);

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

    private void HandlePlayerDead() => GameOverEvents.RaiseGameOver();

    /// <summary>Stops all continuous camera shake when the game ends.</summary>
    private void HandleGameOverEffects() => CameraShake.Instance?.StopContinuousShake();
}
