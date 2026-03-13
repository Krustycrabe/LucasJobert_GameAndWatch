using UnityEngine;

/// <summary>
/// Kamikaze enemy: charges toward the player at high speed, then decelerates progressively
/// as it enters the explosion range. Triggers the explosion animation at close range.
/// If killed by a bullet before reaching the player, plays a death animation instead.
/// </summary>
[RequireComponent(typeof(EnemyCore))]
[RequireComponent(typeof(Animator))]
public class KamikazeBehavior : MonoBehaviour, IEnemyBehavior
{
    [Header("Approach")]
    [Tooltip("Speed multiplier applied on top of EnemyDataSO.moveSpeed during the charge phase.")]
    [SerializeField] private float chargeSpeedMultiplier = 2.2f;

    [Header("Explosion")]
    [Tooltip("Distance at which the explosion animation is triggered.")]
    [SerializeField] private float detectionRange = 4f;
    [Tooltip("Distance at which the enemy starts slowing down (must be >= detectionRange).")]
    [SerializeField] private float slowdownStartRange = 7f;
    [Tooltip("Minimum speed multiplier reached at detectionRange. 0 = full stop, 0.5 = half speed.")]
    [SerializeField] private float minSpeedMultiplierAtRange = 0.3f;
    [Tooltip("Shape of the deceleration curve. 1 = linear, >1 = stays fast longer then drops, <1 = slows early.")]
    [SerializeField] private float decelerationCurve = 1.8f;

    [Header("Explosion Damage")]
    [Tooltip("World-space radius used by the overlap check at the explosion peak frame.")]
    [SerializeField] private float explosionRadius = 1.5f;
    [Tooltip("Camera shake triggered at the peak of the explosion.")]
    [SerializeField] private ShakeData explosionShake = new ShakeData(0.14f, 0.30f);

    private static readonly int StartExplodeHash = Animator.StringToHash("StartExplode");
    private static readonly int DeadHash         = Animator.StringToHash("Dead");

    private EnemyCore _core;
    private Animator _animator;
    private Transform _playerTransform;
    private bool _isPrepping;
    private bool _explodeDone;
    private readonly Collider2D[] _explosionBuffer = new Collider2D[8];

    /// <summary>True from the moment the explosion animation starts until the explosion peak frame.</summary>
    public bool IsPrepping => _isPrepping;

    public void Initialize(EnemyCore core)
    {
        _core = core;
        _animator = GetComponent<Animator>();
    }

    public void OnUpdate()
    {
        if (_explodeDone) return;

        CachePlayer();
        if (_playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, _playerTransform.position);

        if (!_isPrepping && distance <= detectionRange)
        {
            _isPrepping = true;
            _animator.SetTrigger(StartExplodeHash);
            // Keep OnUpdate() running after ForceKill so the enemy keeps moving
            // toward the player during the explosion animation.
            _core.SetKeepUpdatingAfterDeath(true);
            _core.ForceKill();
        }

        float speed = ComputeSpeed(distance);
        transform.position = Vector2.MoveTowards(
            transform.position,
            _playerTransform.position,
            speed * Time.deltaTime);
    }

    public void OnDeath()
    {
        if (!_isPrepping)
        {
            // Killed by bullet before reaching detection range — play death anim.
            // Camera shake and other feedbacks are handled by EnemyCore via deathFeedback SO.
            _animator.SetTrigger(DeadHash);
            _core.DestroyWithDelay(1f);
        }
        // Killed by ForceKill (reached player): StartExplode anim plays.
        // Player damage fires via Animation Event (OnExplosionFrame).
    }

    /// <summary>
    /// Called by Animation Event at the visual peak of the explosion.
    /// Applies a circle overlap damage check for the player and triggers cam shake.
    /// Stops further movement after the explosion peak.
    /// </summary>
    public void OnExplosionFrame()
    {
        _explodeDone = true;
        _core.SetKeepUpdatingAfterDeath(false);

        // Damage player if inside explosion radius.
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, explosionRadius, _explosionBuffer);
        for (int i = 0; i < count; i++)
        {
            if (_explosionBuffer[i] == null) continue;
            if (!_explosionBuffer[i].CompareTag("Player")) continue;
            _explosionBuffer[i].GetComponent<PlayerHealth>()?.TakeDamage(1, PlayerHealth.DamageSource.Explosion);
            break;
        }

        CameraShake.Instance?.Shake(explosionShake);
    }

    /// <summary>
    /// Computes the current movement speed.
    /// Outside slowdownStartRange: full charge speed.
    /// Between slowdownStartRange and detectionRange: smooth progressive deceleration.
    /// Inside detectionRange (prepping): minimum speed so the enemy still drifts into the player.
    /// </summary>
    private float ComputeSpeed(float distance)
    {
        float baseSpeed = _core.Data.moveSpeed * chargeSpeedMultiplier;

        if (distance >= slowdownStartRange)
            return baseSpeed;

        if (distance <= detectionRange)
            return baseSpeed * minSpeedMultiplierAtRange;

        // Inside slowdown zone: interpolate with deceleration curve.
        float t = Mathf.InverseLerp(slowdownStartRange, detectionRange, distance);
        float curved = Mathf.Pow(t, decelerationCurve);
        return Mathf.Lerp(baseSpeed, baseSpeed * minSpeedMultiplierAtRange, curved);
    }

    private void CachePlayer()
    {
        if (_playerTransform != null) return;
        var player = GameObject.FindWithTag("Player");
        if (player != null) _playerTransform = player.transform;
    }
}
