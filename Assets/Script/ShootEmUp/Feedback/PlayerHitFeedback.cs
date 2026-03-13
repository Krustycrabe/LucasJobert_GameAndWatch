using UnityEngine;

/// <summary>
/// Orchestrates all feedback systems when the player takes a hit.
/// Routes to a specific FeedbackConfigSO per damage source (bullet, spear, explosion, contact).
/// Delegates to each dedicated singleton (HitStopManager, CameraShake, CameraZoomFeedback, VignetteFeedback).
/// </summary>
public class PlayerHitFeedback : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("VFX")]
    [Tooltip("Animator on the HitVFX child GameObject.")]
    [SerializeField] private Animator hitVfxAnimator;
    [Tooltip("Exact name of the trigger parameter in the HitVFX AnimatorController.")]
    [SerializeField] private string hitTriggerName = "PlayerHit";

    [Header("Feedback Configs — per damage source")]
    [SerializeField] private FeedbackConfigSO contactConfig;
    [SerializeField] private FeedbackConfigSO bulletConfig;
    [SerializeField] private FeedbackConfigSO spearConfig;
    [SerializeField] private FeedbackConfigSO explosionConfig;
    [SerializeField] private FeedbackConfigSO deathConfig;

    private int _hitTriggerHash;

    private void Awake()
    {
        _hitTriggerHash = Animator.StringToHash(hitTriggerName);
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged += HandleDamaged;
            playerHealth.OnDead    += HandleDead;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged -= HandleDamaged;
            playerHealth.OnDead    -= HandleDead;
        }
    }

    private void HandleDamaged(int remainingLives, PlayerHealth.DamageSource source)
    {
        FeedbackConfigSO config = source switch
        {
            PlayerHealth.DamageSource.Bullet    => bulletConfig,
            PlayerHealth.DamageSource.Spear     => spearConfig,
            PlayerHealth.DamageSource.Explosion => explosionConfig,
            _                                   => contactConfig
        };
        TriggerFeedback(config);
    }

    private void HandleDead() => TriggerFeedback(deathConfig);

    /// <summary>
    /// Triggers all feedback effects described in <paramref name="config"/>.
    /// Call from any external system (e.g. laser hit) by passing the appropriate config.
    /// </summary>
    public void TriggerFeedback(FeedbackConfigSO config)
    {
        if (config == null) return;

        hitVfxAnimator?.SetTrigger(_hitTriggerHash);

        HitStopManager.Instance?.FreezeFrame(config.hitStopDuration);

        if (config.cameraShake.magnitude > 0f)
            CameraShake.Instance?.Shake(config.cameraShake);

        CameraZoomFeedback.Instance?.Punch(config.zoomAmount, config.zoomDuration);
        VignetteFeedback.Instance?.Flash(config.vignetteIntensity, config.vignetteDuration);
    }
}
