using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton that freezes time for a short duration to punctuate impactful events.
/// Uses unscaled time so the coroutine continues while timeScale is 0.
/// Call FreezeFrame() from any feedback orchestrator or game system.
/// </summary>
public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    private Coroutine _currentFreeze;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Freezes gameplay for <paramref name="duration"/> seconds (unscaled).
    /// If a freeze is already running, the longer one wins.
    /// </summary>
    public void FreezeFrame(float duration)
    {
        if (duration <= 0f) return;

        if (_currentFreeze != null)
        {
            // Keep the longer freeze active.
            if (duration <= _remainingFreeze) return;
            StopCoroutine(_currentFreeze);
            Time.timeScale = 0f;
        }

        _currentFreeze = StartCoroutine(FreezeRoutine(duration));
    }

    private float _remainingFreeze;

    private IEnumerator FreezeRoutine(float duration)
    {
        _remainingFreeze = duration;
        Time.timeScale = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _remainingFreeze = duration - elapsed;
            yield return null;
        }

        Time.timeScale = 1f;
        _remainingFreeze = 0f;
        _currentFreeze = null;
    }
}
