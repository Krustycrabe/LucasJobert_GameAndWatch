using System.Collections;
using UnityEngine;

/// <summary>
/// Kamikaze enemy. Charges the player. On reaching detectionRange, triggers the
/// explosion animation. Damage is dealt via a growing CircleCollider2D (trigger)
/// that expands over the explosion duration — this lets PlayerHealth.OnTriggerEnter2D
/// handle the hit through the normal contact path with no special cases.
///
/// Explosion lifecycle:
///   1. distance <= detectionRange → BeginExplosion(), body collider disabled, SetTrigger(StartExplode)
///   2. Animation Event OnExplosionStart() → enables and grows explosionCollider via coroutine
///   3. Player enters growing collider → PlayerHealth.OnTriggerEnter2D → TakeDamage(Contact)
///   4. Coroutine ends → disable explosionCollider → ResolveExplosionDeath() → Destroy
///
/// If killed by bullet before reaching range: OnDeath() → Dead anim → DestroyWithDelay.
/// </summary>
[RequireComponent(typeof(EnemyCore))]
[RequireComponent(typeof(Animator))]
public class KamikazeBehavior : MonoBehaviour, IEnemyBehavior
{
    [Header("Approach")]
    [SerializeField] private float chargeSpeedMultiplier = 2.2f;
    [SerializeField] private float slowdownStartRange    = 7f;
    [SerializeField] private float detectionRange        = 4f;
    [SerializeField] private float minSpeedMultiplier    = 0.3f;
    [SerializeField] private float decelerationCurve     = 1.8f;

    [Header("Explosion Collider")]
    [Tooltip("The CircleCollider2D on the ExplosionZone child GO (tag: Enemy, isTrigger: true). Grows during explosion.")]
    [SerializeField] private CircleCollider2D explosionCollider;
    [Tooltip("Final radius of the explosion collider at full expansion.")]
    [SerializeField] private float explosionMaxRadius = 2.5f;
    [Tooltip("Time in seconds to grow from 0 to max radius.")]
    [SerializeField] private float explosionGrowDuration = 0.4f;
    [Tooltip("Extra seconds to wait after the collider finishes growing before destroying.")]
    [SerializeField] private float explosionLingerDuration = 0.3f;
    [Tooltip("Camera shake at the start of the explosion.")]
    [SerializeField] private ShakeData explosionShake = new ShakeData(0.14f, 0.30f);

    private static readonly int StartExplodeHash = Animator.StringToHash("StartExplode");
    private static readonly int DeadHash         = Animator.StringToHash("Dead");

    private EnemyCore  _core;
    private Animator   _animator;
    private Transform  _playerTransform;
    private bool _isPrepping;
    private bool _growStarted;

    public void Initialize(EnemyCore core)
    {
        _core     = core;
        _animator = GetComponent<Animator>();

        if (explosionCollider != null)
            explosionCollider.enabled = false;
    }

    public void OnUpdate()
    {
        if (_isPrepping) return;

        CachePlayer();
        if (_playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, _playerTransform.position);

        if (distance <= detectionRange)
        {
            _isPrepping = true;
            _core.BeginExplosion();           // disables body collider, sets _isExploding
            _animator.SetTrigger(StartExplodeHash);
            // Movement stops; the Animation Event OnExplosionStart() starts the collider growth.
            return;
        }

        float speed = ComputeSpeed(distance);
        transform.position = Vector2.MoveTowards(
            transform.position,
            _playerTransform.position,
            speed * Time.deltaTime);
    }

    public void OnDeath()
    {
        // Called only when killed by a bullet before reaching detectionRange.
        if (_isPrepping) return;
        _animator.SetTrigger(DeadHash);
        _core.DestroyWithDelay(1f);
    }

    /// <summary>
    /// Animation Event — called on the first frame of the explosion animation clip.
    /// Starts the collider growth coroutine. Guarded against double-start.
    /// </summary>
    public void OnExplosionStart()
    {
        if (_growStarted) return;
        _growStarted = true;
        CameraShake.Instance?.Shake(explosionShake);
        StartCoroutine(GrowExplosionCollider());
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private IEnumerator GrowExplosionCollider()
    {
        if (explosionCollider == null)
        {
            _core.ResolveExplosionDeath();
            yield break;
        }

        explosionCollider.radius  = 0f;
        explosionCollider.enabled = true;

        float elapsed = 0f;
        while (elapsed < explosionGrowDuration)
        {
            elapsed += Time.deltaTime;
            explosionCollider.radius = Mathf.Lerp(0f, explosionMaxRadius, elapsed / explosionGrowDuration);
            yield return null;
        }

        explosionCollider.radius = explosionMaxRadius;

        if (explosionLingerDuration > 0f)
            yield return new WaitForSeconds(explosionLingerDuration);

        explosionCollider.enabled = false;
        _core.ResolveExplosionDeath();
    }

    private float ComputeSpeed(float distance)
    {
        float baseSpeed = _core.Data.moveSpeed * chargeSpeedMultiplier;
        if (distance >= slowdownStartRange) return baseSpeed;
        if (distance <= detectionRange)     return baseSpeed * minSpeedMultiplier;
        float t = Mathf.InverseLerp(slowdownStartRange, detectionRange, distance);
        return Mathf.Lerp(baseSpeed, baseSpeed * minSpeedMultiplier, Mathf.Pow(t, decelerationCurve));
    }

    private void CachePlayer()
    {
        if (_playerTransform != null) return;
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) _playerTransform = p.transform;
    }
}


