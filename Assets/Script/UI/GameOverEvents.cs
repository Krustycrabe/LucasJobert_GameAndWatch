using System;

/// <summary>
/// Neutral static event channel shared across all levels.
/// Any game system raises events here; GameOverScreen listens here regardless of context.
/// To add a new level: create a bridge script that calls RaiseGameOver() and RaiseScoreUpdated().
/// </summary>
public static class GameOverEvents
{
    /// <summary>Fires when all lives are lost and the game is definitively over.</summary>
    public static event Action OnGameOver;

    /// <summary>Fires whenever the score changes. Carries the new score value.</summary>
    public static event Action<int> OnScoreUpdated;

    /// <summary>Broadcasts the game over state to all listeners.</summary>
    public static void RaiseGameOver() => OnGameOver?.Invoke();

    /// <summary>Broadcasts a score update to all listeners.</summary>
    public static void RaiseScoreUpdated(int score) => OnScoreUpdated?.Invoke(score);

    /// <summary>
    /// Clears all subscribers from both events.
    /// Call this from a scene-scoped MonoBehaviour's OnDestroy to prevent stale
    /// static subscribers from firing into a freshly loaded scene.
    /// </summary>
    public static void Reset()
    {
        OnGameOver     = null;
        OnScoreUpdated = null;
    }
}
