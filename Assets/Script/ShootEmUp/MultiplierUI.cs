using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Drives the multiplier TextMeshPro display.
/// - Kill punch: small scale pop each time the multiplier increments.
/// - Shake: continuous positional shake that grows with the multiplier value
///   (starts at multiplierShakeThreshold, peaks at maxMultiplier).
/// - Warning pulse: accelerating scale oscillation when the multiplier is about to reset.
///   The pulse frequency increases as the timeout approaches, giving a strong visual cue.
/// Place this component on the ScorePanel (or any persistent UI parent).
/// </summary>
public class MultiplierUI : MonoBehaviour
{
    [SerializeField] private GameDataSO gameData;
    [SerializeField] private SmUpScoreManager scoreManager;
    [SerializeField] private TextMeshProUGUI multiplierText;

    private float _warningUrgency;
    private float _pulsePhase;
    private float _shakePhase;
    private int   _currentMultiplier = 1;
    private Coroutine _killPunchCoroutine;
    private bool _killPunchRunning;
    private Vector3 _baseLocalPosition;

    private void Awake()
    {
        _baseLocalPosition = multiplierText.transform.localPosition;
    }

    private void OnEnable()
    {
        scoreManager.OnMultiplierChanged += HandleMultiplierChanged;
        scoreManager.OnMultiplierWarning += HandleMultiplierWarning;
        scoreManager.OnMultiplierReset   += HandleMultiplierReset;
    }

    private void OnDisable()
    {
        scoreManager.OnMultiplierChanged -= HandleMultiplierChanged;
        scoreManager.OnMultiplierWarning -= HandleMultiplierWarning;
        scoreManager.OnMultiplierReset   -= HandleMultiplierReset;
    }

    private void Update()
    {
        ApplyShake();

        if (_warningUrgency <= 0f || _killPunchRunning) return;

        // Accelerating pulse: frequency ramps from warningPulseSpeed to warningPulseSpeed × maxMultiplier.
        float speedMult = Mathf.Lerp(1f, gameData.warningMaxPulseMultiplier, _warningUrgency);
        _pulsePhase += gameData.warningPulseSpeed * speedMult * Time.deltaTime;
        float pulse  = Mathf.Abs(Mathf.Sin(_pulsePhase));
        float scale  = 1f + pulse * gameData.warningPulseAmplitude;
        multiplierText.transform.localScale = Vector3.one * scale;
    }

    // ── Event handlers ─────────────────────────────────────────────────────────

    private void HandleMultiplierChanged(int multiplier)
    {
        _currentMultiplier = multiplier;
        multiplierText.text = $"x{multiplier}";

        if (_killPunchCoroutine != null) StopCoroutine(_killPunchCoroutine);
        _killPunchCoroutine = StartCoroutine(KillPunchAnimation());
    }

    private void HandleMultiplierWarning(float urgency)
    {
        _warningUrgency = urgency;
    }

    private void HandleMultiplierReset()
    {
        _currentMultiplier = 1;
        _warningUrgency    = 0f;
        _pulsePhase        = 0f;
        if (!_killPunchRunning)
            multiplierText.transform.localScale = Vector3.one;
    }

    // ── Animations ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Continuous positional shake scaled by how far the multiplier is above the threshold.
    /// Intensity is 0 at threshold and 1 at maxMultiplier.
    /// </summary>
    private void ApplyShake()
    {
        int threshold = gameData.multiplierShakeThreshold;
        if (_currentMultiplier < threshold)
        {
            multiplierText.transform.localPosition = _baseLocalPosition;
            return;
        }

        float intensity = Mathf.InverseLerp(threshold, gameData.maxMultiplier, _currentMultiplier);
        float offset    = intensity * gameData.multiplierShakeMaxOffset;

        _shakePhase += gameData.multiplierShakeFrequency * Time.deltaTime;
        // Two perpendicular sin waves at slightly different frequencies for organic feel.
        float x = Mathf.Sin(_shakePhase * 1.00f) * offset;
        float y = Mathf.Sin(_shakePhase * 1.31f) * offset;

        multiplierText.transform.localPosition = _baseLocalPosition + new Vector3(x, y, 0f);
    }

    /// <summary>
    /// Quick scale pop on every multiplier increment.
    /// Pauses the warning pulse while it plays so the two don't fight.
    /// </summary>
    private IEnumerator KillPunchAnimation()
    {
        _killPunchRunning = true;
        float elapsed  = 0f;
        float duration = gameData.multiplierKillPunchDuration;
        float amount   = gameData.multiplierKillPunch;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / duration;
            multiplierText.transform.localScale = Vector3.one * (1f + amount * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        if (_warningUrgency <= 0f)
            multiplierText.transform.localScale = Vector3.one;

        _killPunchRunning   = false;
        _killPunchCoroutine = null;
    }
}
