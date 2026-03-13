using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton. Applies a near-miss slow motion effect by scaling Time.timeScale.
/// Uses unscaled real time for its own coroutine so the recovery is not affected by the
/// time scale change. All gameplay systems that use Time.deltaTime slow down automatically.
/// </summary>
public class SlowMotionManager : MonoBehaviour
{
    public static SlowMotionManager Instance { get; private set; }

    private VirusSplitConfigSO _config;
    private Coroutine _activeCoroutine;
    private bool _active;

    public void Initialize(VirusSplitConfigSO config)
    {
        _config = config;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f; // safety reset
        }
    }

    /// <summary>
    /// Triggers a slow-motion plateau followed by a smooth recovery back to normal speed.
    /// Safe to call multiple times — restarts the effect cleanly.
    /// </summary>
    public void TriggerSlowMotion()
    {
        if (_config == null) return;
        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        _activeCoroutine = StartCoroutine(SlowMotionRoutine());
    }

    private IEnumerator SlowMotionRoutine()
    {
        _active = true;
        Time.timeScale = _config.slowMotionScale;

        yield return new WaitForSecondsRealtime(_config.slowMotionDuration);

        float elapsed  = 0f;
        float recovery = _config.slowMotionRecovery;
        float start    = _config.slowMotionScale;

        while (elapsed < recovery)
        {
            elapsed        += Time.unscaledDeltaTime;
            Time.timeScale  = Mathf.Lerp(start, 1f, elapsed / recovery);
            yield return null;
        }

        Time.timeScale   = 1f;
        _active          = false;
        _activeCoroutine = null;
    }
}
