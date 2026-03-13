using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tick-based laser hitbox. Enabled and disabled by PlayerShooter for the duration of the laser.
/// Attach to a child GameObject of the player/shoot arm that has a BoxCollider2D (Is Trigger = true)
/// sized and positioned to match the laser visual.
/// Damages all enemies inside the collider at a configurable rate via EnemyCore.TakeLaserDamage.
/// Also drives the CameraShake continuous shake while active.
/// A FeedbackConfigSO can be assigned to trigger screen feedback on each damage tick.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LaserHitbox : MonoBehaviour
{
    [SerializeField] private int damagePerTick = 1;
    [Tooltip("Seconds between each damage tick.")]
    [SerializeField] private float tickRate = 0.12f;
    [Tooltip("Feedback triggered on the player side at each damage tick. Assign your laser FeedbackConfigSO here.")]
    [SerializeField] private FeedbackConfigSO laserTickFeedback;

    private Collider2D _collider;
    private float _tickTimer;
    private readonly List<Collider2D> _hitBuffer = new List<Collider2D>();
    private ContactFilter2D _filter;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        // Use trigger overlap only. Enemy filtering is done by tag ("Enemy") in ApplyDamageTick.
        // No layer mask needed — "Ennemi" is a sorting layer, not a physics layer, so
        // LayerMask.GetMask("Ennemi") would return 0 and block all detections.
        _filter.useTriggers = true;
        _collider.enabled = false;
        enabled = false;
    }

    private void OnEnable()
    {
        _collider.enabled = true;
        _tickTimer = 0f;
        CameraShake.Instance?.StartContinuousShake(CameraShake.Instance.LaserContinuousShake);
    }

    private void OnDisable()
    {
        _collider.enabled = false;
        CameraShake.Instance?.StopContinuousShake();
    }

    private void Update()
    {
        _tickTimer += Time.deltaTime;
        if (_tickTimer < tickRate) return;
        _tickTimer = 0f;
        ApplyDamageTick();
    }

    /// <summary>
    /// Deals one tick of laser damage to every EnemyCore inside the collider bounds
    /// and triggers the laser feedback config if at least one enemy was hit.
    /// </summary>
    private void ApplyDamageTick()
    {
        int count = _collider.Overlap(_filter, _hitBuffer);
        bool hitAny = false;

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _hitBuffer[i];
            if (col == null || !col.CompareTag("Enemy")) continue;

            EnemyCore core = col.GetComponentInParent<EnemyCore>();
            if (core == null) continue;

            core.TakeLaserDamage(damagePerTick);
            hitAny = true;
        }

        if (hitAny)
            TriggerLaserFeedback();
    }

    private void TriggerLaserFeedback()
    {
        if (laserTickFeedback == null) return;
        HitStopManager.Instance?.FreezeFrame(laserTickFeedback.hitStopDuration);
        if (laserTickFeedback.cameraShake.magnitude > 0f)
            CameraShake.Instance?.Shake(laserTickFeedback.cameraShake);
        CameraZoomFeedback.Instance?.Punch(laserTickFeedback.zoomAmount, laserTickFeedback.zoomDuration);
        VignetteFeedback.Instance?.Flash(laserTickFeedback.vignetteIntensity, laserTickFeedback.vignetteDuration);
    }
}
