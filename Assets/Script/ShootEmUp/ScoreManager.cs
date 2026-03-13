using System;
using UnityEngine;

/// <summary>
/// Singleton that owns the score and kill-multiplier state for the ShootEmUp game.
/// Drives passive time-based scoring and processes kill bonuses with the current multiplier.
/// </summary>
public class SmUpScoreManager : MonoBehaviour
{
    public static SmUpScoreManager Instance { get; private set; }

    [SerializeField] private GameDataSO gameData;

    // ── Events ─────────────────────────────────────────────────────────────────

    /// <summary>Fires every time the actual score changes (time tick or kill bonus).</summary>
    public event Action<int> OnScoreChanged;

    /// <summary>
    /// Fires when a kill bonus is awarded. Carries the total amount added (base × multiplier)
    /// and the new total score. Used to trigger punch animations.
    /// </summary>
    public event Action<int, int> OnInstantScoreAdded;

    /// <summary>Fires when the multiplier value changes (increment or reset).</summary>
    public event Action<int> OnMultiplierChanged;

    /// <summary>Fires every frame while the multiplier warning is active. Urgency goes 0 → 1.</summary>
    public event Action<float> OnMultiplierWarning;

    /// <summary>Fires when the multiplier resets to x1.</summary>
    public event Action OnMultiplierReset;

    // ── State ──────────────────────────────────────────────────────────────────

    private int _actualScore;
    private float _timeScoreAccum;

    private int _multiplier = 1;
    private float _multiplierTimer;
    private bool _multiplierActive;

    public int ActualScore   => _actualScore;
    public int Multiplier    => _multiplier;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _multiplier = 1;
    }

    private void Update()
    {
        TickPassiveScore();
        TickMultiplier();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by EnemyCore on enemy death.
    /// Applies the current multiplier, awards the score, then increments the multiplier.
    /// </summary>
    public void AwardKillScore(int baseScore)
    {
        int total = baseScore * _multiplier;
        _actualScore += total;
        OnScoreChanged?.Invoke(_actualScore);
        OnInstantScoreAdded?.Invoke(total, _actualScore);
        IncrementMultiplier();
    }

    /// <summary>Increments the multiplier by 1 (capped at max) and restarts the decay timer.</summary>
    public void IncrementMultiplier()
    {
        _multiplier = Mathf.Min(_multiplier + 1, gameData.maxMultiplier);
        _multiplierTimer  = gameData.multiplierTimeout;
        _multiplierActive = true;
        OnMultiplierChanged?.Invoke(_multiplier);
    }

    /// <summary>Resets the multiplier to x1. Called on player damage.</summary>
    public void ResetMultiplier()
    {
        if (_multiplier == 1) return;
        _multiplier       = 1;
        _multiplierTimer  = 0f;
        _multiplierActive = false;
        OnMultiplierChanged?.Invoke(_multiplier);
        OnMultiplierReset?.Invoke();
    }

    // ── Internal ───────────────────────────────────────────────────────────────

    private void TickPassiveScore()
    {
        _timeScoreAccum += gameData.scorePerSecond * Time.deltaTime;
        int pts = Mathf.FloorToInt(_timeScoreAccum);
        if (pts <= 0) return;
        _actualScore     += pts;
        _timeScoreAccum  -= pts;
        OnScoreChanged?.Invoke(_actualScore);
    }

    private void TickMultiplier()
    {
        if (!_multiplierActive || _multiplier <= 1) return;

        _multiplierTimer -= Time.deltaTime;

        // Fire warning event with urgency 0→1 as timer approaches 0.
        float remaining = _multiplierTimer / gameData.multiplierTimeout;
        if (remaining < gameData.warningThreshold)
        {
            float urgency = Mathf.InverseLerp(gameData.warningThreshold, 0f, remaining);
            OnMultiplierWarning?.Invoke(urgency);
        }

        if (_multiplierTimer <= 0f)
            ResetMultiplier();
    }
}
