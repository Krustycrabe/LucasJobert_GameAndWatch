using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton. Applies a timed near-miss slow-motion effect.
///
/// Trigger model — one-shot on player input:
///   1. ObstacleNearMissTrigger calls SetProximity(true/false) when a virus
///      enters or leaves an obstacle's proximity zone.
///   2. VirusController calls TryTriggerSlowMo() on every split or merge INPUT.
///   3. Slow-mo fires ONLY IF a virus is currently inside a proximity zone
///      at the moment the input occurs.
///   4. Effect runs for slowMotionDuration (real time), then lerps back to 1.
///   5. A new trigger during recovery or plateau restarts the plateau timer.
/// </summary>
public class SlowMotionManager : MonoBehaviour
{
    public static SlowMotionManager Instance { get; private set; }

    private VirusSplitConfigSO _config;
    private bool               _proximityActive;
    private Coroutine          _activeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance       = null;
            Time.timeScale = 1f;
        }
    }

    // ── Initialisation ─────────────────────────────────────────────────────────

    /// <summary>Called by VirusController at Start().</summary>
    public void Initialize(VirusSplitConfigSO config, Func<bool> getIsSplit, Func<Vector2[]> getVirusPositions)
    {
        _config = config;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by ObstacleNearMissTrigger when a virus enters (true) or exits (false)
    /// the obstacle proximity zone.
    /// </summary>
    public void SetProximity(bool active) => _proximityActive = active;

    /// <summary>
    /// Called by VirusController on every split or merge input.
    /// Starts slow-mo only if a virus is currently inside a proximity zone.
    /// </summary>
    public void TryTriggerSlowMo()
    {
        if (_config == null || !_proximityActive) return;

        // Restart the routine — resets the plateau timer even if already active.
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(SlowMotionRoutine());
    }

    // ── Internal ───────────────────────────────────────────────────────────────

    private IEnumerator SlowMotionRoutine()
    {
        // Plateau
        Time.timeScale = _config.slowMotionScale;
        yield return new WaitForSecondsRealtime(_config.slowMotionDuration);

        // Recovery lerp (unscaled so it's not affected by the current timeScale)
        float start   = Time.timeScale;
        float elapsed = 0f;

        while (elapsed < _config.slowMotionRecovery)
        {
            elapsed        += Time.unscaledDeltaTime;
            Time.timeScale  = Mathf.Lerp(start, 1f, elapsed / _config.slowMotionRecovery);
            yield return null;
        }

        Time.timeScale = 1f;
        _activeRoutine = null;
    }
}

