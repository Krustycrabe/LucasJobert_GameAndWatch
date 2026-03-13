using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tick-based laser hitbox. Enabled and disabled by PlayerShooter for the duration of the laser.
/// Attach to a child GameObject of the player/shoot arm that has a BoxCollider2D (Is Trigger = true)
/// sized and positioned to match the laser visual.
/// Damages all enemies inside the collider at a configurable rate.
/// Also drives the CameraShake continuous shake while active.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LaserHitbox : MonoBehaviour
{
    [SerializeField] private int damagePerTick = 1;
    [Tooltip("Seconds between each damage tick.")]
    [SerializeField] private float tickRate = 0.12f;

    private Collider2D _collider;
    private float _tickTimer;
    private readonly List<Collider2D> _hitBuffer = new List<Collider2D>();
    private readonly ContactFilter2D _filter = new ContactFilter2D { useTriggers = true };

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        // Disable both the MonoBehaviour AND the physics collider.
        // Disabling only 'enabled' leaves the Collider2D physically active, which causes
        // OnTriggerEnter2D to propagate up to the parent Rigidbody2D (PlayerHealth) —
        // making any enemy entering the laser zone deal contact damage to the player.
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

    /// <summary>Deals one tick of damage to every EnemyCore inside the collider bounds.</summary>
    private void ApplyDamageTick()
    {
        int count = _collider.Overlap(_filter, _hitBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D col = _hitBuffer[i];
            if (col == null || !col.CompareTag("Enemy")) continue;
            // GetComponentInParent handles cases where the collider is on a child of the EnemyCore root.
            col.GetComponentInParent<EnemyCore>()?.TakeDamage(damagePerTick);
        }
    }
}
