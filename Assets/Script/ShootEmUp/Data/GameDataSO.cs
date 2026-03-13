using UnityEngine;

/// <summary>
/// Central ScriptableObject for all player and UI system configuration.
/// Create one instance via Assets > Create > ShootEmUp > Game Data.
/// </summary>
[CreateAssetMenu(fileName = "GameData", menuName = "ShootEmUp/Game Data")]
public class GameDataSO : ScriptableObject
{
    [Header("Player — Lives")]
    [Tooltip("Starting number of lives. Also controls how many hearts are visible initially.")]
    public int maxLives = 5;
    [Tooltip("Seconds of invincibility after taking a hit.")]
    public float invincibilityDuration = 1.5f;

    [Header("Life Panel — UI")]
    [Tooltip("Pixels to slide LifePanel right per life lost.")]
    public float lifePanelStepX = 101f;
    [Tooltip("Lerp speed for the LifePanel slide animation.")]
    public float lifePanelLerpSpeed = 8f;

    [Header("Score — Passive")]
    [Tooltip("Points added per second passively, regardless of kills.")]
    public float scorePerSecond = 10f;

    [Header("Score — Display Counter")]
    [Tooltip("Minimum display counter speed in points per second (keeps up with passive rate).")]
    public float minCountupRate = 20f;
    [Tooltip("Multiplier applied to the gap between actual and displayed score.")]
    public float countupSpeedFactor = 2.5f;

    [Header("Score — Punch Animation")]
    [Tooltip("Minimum instant score gain (after multiplier) that triggers the punch animation.")]
    public int punchThreshold = 50;
    [Tooltip("Score gain considered maximum for scaling the punch magnitude. Gains above this use max punch.")]
    public int punchMaxGain = 500;
    [Tooltip("Maximum scale additive for the punch (e.g. 0.35 → scale goes to 1.35 at peak).")]
    public float maxScorePunch = 0.35f;
    [Tooltip("Maximum camera shake magnitude triggered by a large score gain.")]
    public float maxScoreShake = 0.05f;
    [Tooltip("Duration of the punch animation in seconds.")]
    public float punchDuration = 0.25f;

    [Header("Multiplier")]
    [Tooltip("Maximum multiplier value the player can reach.")]
    public int maxMultiplier = 8;
    [Tooltip("Seconds without a kill before the multiplier resets to x1.")]
    public float multiplierTimeout = 4f;
    [Tooltip("Fraction of timeout remaining at which the warning animation starts (0 = disabled, 1 = always).")]
    [Range(0f, 1f)]
    public float warningThreshold = 0.45f;
    [Tooltip("Base angular speed (radians per second) of the warning pulse.")]
    public float warningPulseSpeed = 4f;
    [Tooltip("Speed multiplier at the moment of reset (pulse accelerates from 1x to this value).")]
    public float warningMaxPulseMultiplier = 5f;
    [Tooltip("Scale amplitude of the warning pulse (e.g. 0.12 → ±12% scale oscillation).")]
    public float warningPulseAmplitude = 0.12f;
    [Tooltip("Scale additive for the kill-punch animation on the multiplier text.")]
    public float multiplierKillPunch = 0.18f;
    [Tooltip("Duration of the kill-punch animation on the multiplier text.")]
    public float multiplierKillPunchDuration = 0.15f;

    [Header("Multiplier — Shake")]
    [Tooltip("Shake starts at this multiplier value or above (e.g. 3 → shake kicks in at x3).")]
    public int multiplierShakeThreshold = 3;
    [Tooltip("Maximum pixel offset of the shake at the highest multiplier value.")]
    public float multiplierShakeMaxOffset = 4f;
    [Tooltip("Shake oscillation frequency in Hz.")]
    public float multiplierShakeFrequency = 24f;
}
