using System;
using UnityEngine;

/// <summary>
/// Singleton. Tracks the distance travelled in metres and fires events for the UI.
/// Distance is accumulated each frame from the current scroll speed.
/// Reuses GameOverEvents.RaiseScoreUpdated so the generic GameOverScreen can display
/// the final score without modification.
/// </summary>
public class VirusScoreManager : MonoBehaviour
{
    public static VirusScoreManager Instance { get; private set; }

    /// <summary>Fires every frame with the current metre count (rounded down).</summary>
    public event Action<int> OnMetresChanged;

    private VirusSplitConfigSO _config;
    private float _totalDistance; // world units scrolled
    private float _currentSpeed;
    private int   _lastMetres;
    private bool  _running;

    public int CurrentMetres => Mathf.FloorToInt(_totalDistance * _config.metersPerUnit);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()  => GameOverEvents.OnGameOver += HandleGameOver;
    private void OnDisable() => GameOverEvents.OnGameOver -= HandleGameOver;

    /// <summary>Called by VirusController to start score tracking.</summary>
    public void Initialize(VirusSplitConfigSO config)
    {
        _config  = config;
        _running = true;
    }

    /// <summary>Called by VirusController every frame to push the current scroll speed.</summary>
    public void UpdateSpeed(float scrollSpeed) => _currentSpeed = scrollSpeed;

    private void Update()
    {
        if (!_running || _config == null) return;

        _totalDistance += _currentSpeed * Time.deltaTime;

        int metres = CurrentMetres;
        if (metres != _lastMetres)
        {
            _lastMetres = metres;
            OnMetresChanged?.Invoke(metres);
            GameOverEvents.RaiseScoreUpdated(metres);
        }
    }

    private void HandleGameOver()
    {
        _running = false;
        GameOverEvents.RaiseScoreUpdated(CurrentMetres);
    }
}
