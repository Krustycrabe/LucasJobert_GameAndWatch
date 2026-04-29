using System;
using System.Collections;
using UnityEngine;
using GameAndWatch.Audio;

/// <summary>
/// Shared logic for all enemies: health, bullet collision, energy drop, death lifecycle.
/// Delegates per-frame behavior to the IEnemyBehavior component on the same GameObject.
///
/// Kamikaze explosion path:
///   BeginExplosion() → _isExploding=true, body collider disabled.
///   Enemy stays alive (_isDead=false) so the growing explosion CircleCollider2D
///   can trigger PlayerHealth.OnTriggerEnter2D through the standard contact path.
///   After the circle finishes: ResolveExplosionDeath() → destroy, no score, no energy.
///   If killed by bullet before range: normal Die() → score + energy.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnemyCore : MonoBehaviour
{
    private static readonly int OnHitHash = Animator.StringToHash("OnHit");

    [SerializeField] private EnemyDataSO data;
    [Tooltip("Fallback destroy delay when no death animation event is set up.")]
    [SerializeField] private float fallbackDestroyDelay = 0.5f;

    [Header("VFX")]
    [Tooltip("Animator on the VFXHitLaser child. Receives the OnHit trigger on laser hits.")]
    [SerializeField] private Animator vfxAnimator;

    [Header("Feedback")]
    [Tooltip("Feedback triggered on non-lethal bullet hits. Leave empty to skip.")]
    [SerializeField] private FeedbackConfigSO hitFeedback;
    [Tooltip("Feedback triggered when this enemy dies. Leave empty to skip.")]
    [SerializeField] private FeedbackConfigSO deathFeedback;

    public event Action OnDeathEvent;

    public EnemyDataSO Data => data;
    /// <summary>True once Die() or ResolveExplosionDeath() has been called.</summary>
    public bool IsDead      => _isDead;
    /// <summary>True while the kamikaze explosion anim runs. Blocks bullet/laser hits only.</summary>
    public bool IsExploding => _isExploding;

    /// <summary>Move speed after loop scaling is applied. Initialized from Data.moveSpeed.</summary>
    public float RuntimeSpeed         { get; private set; }
    /// <summary>Shoot interval after loop scaling is applied. Initialized from Data.shootRate.</summary>
    public float RuntimeShootInterval { get; private set; }

    private int  _currentHealth;
    private IEnemyBehavior _behavior;
    private bool _isDead;
    private bool _isExploding;

    private void Awake()
    {
        _currentHealth        = data.maxHealth;
        RuntimeSpeed          = data.moveSpeed;
        RuntimeShootInterval  = data.shootRate;
        _behavior = GetComponent<IEnemyBehavior>();
        _behavior?.Initialize(this);
    }

    /// <summary>
    /// Applies cumulative loop scaling to runtime stats.
    /// Call once immediately after instantiation from the spawner.
    /// </summary>
    public void ApplyScaling(float speedMultiplier, float shootIntervalMultiplier)
    {
        RuntimeSpeed         *= speedMultiplier;
        RuntimeShootInterval *= shootIntervalMultiplier;
    }

    private void OnEnable()  => GameOverEvents.OnGameOver += HandleGameOver;
    private void OnDisable() => GameOverEvents.OnGameOver -= HandleGameOver;

    private void Update()
    {
        if (!_isDead) _behavior?.OnUpdate();
    }

    private void HandleGameOver()
    {
        if (_isDead) return;
        _isDead = true;
        _behavior?.OnDeath();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDead || _isExploding) return;
        if (!other.CompareTag("PlayerBullet")) return;

        BulletMover bullet = other.GetComponent<BulletMover>();
        if (bullet == null || bullet.HasHit) return;

        bullet.TriggerHit();
        TakeDamage(bullet.Damage);
    }

    /// <summary>Reduces health from a bullet hit. Triggers hitFeedback on non-lethal hits.</summary>
    public void TakeDamage(int amount)
    {
        if (_isDead || _isExploding) return;
        _currentHealth -= amount;
        AudioManager.Instance?.PlayOneShot(SoundIds.ShootEmUp.EnemyHit);
        if (_currentHealth <= 0) Die();
        else TriggerFeedback(hitFeedback);
    }

    /// <summary>Reduces health from a laser tick. Plays the OnHit VFX trigger. No energy drop on kill.</summary>
    public void TakeLaserDamage(int amount)
    {
        if (_isDead || _isExploding) return;
        _currentHealth -= amount;
        AudioManager.Instance?.PlayOneShot(SoundIds.ShootEmUp.EnemyHit);
        vfxAnimator?.SetTrigger(OnHitHash);
        if (_currentHealth <= 0) Die(dropEnergy: false);
    }

    /// <summary>
    /// Called by KamikazeBehavior at explosion start.
    /// Sets _isExploding (blocks bullet/laser) but NOT _isDead, so the growing
    /// explosion CircleCollider2D on the child GO can trigger PlayerHealth normally.
    /// Disables the root body collider to prevent accidental contact damage.
    /// </summary>
    public void BeginExplosion()
    {
        if (_isDead) return;
        _isExploding = true;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    /// <summary>
    /// Called by KamikazeBehavior after the explosion collider finishes growing.
    /// No score, no energy: kamikaze self-destructs, player did not kill it.
    /// </summary>
    public void ResolveExplosionDeath()
    {
        if (_isDead) return;
        _isDead      = true;
        _isExploding = false;
        TriggerFeedback(deathFeedback);
        OnDeathEvent?.Invoke();
        Destroy(gameObject);
    }

    private void Die(bool dropEnergy = true)
    {
        _isDead = true;
        if (dropEnergy) SpawnEnergy();
        SmUpScoreManager.Instance?.AwardKillScore(data.scoreValue);
        OnDeathEvent?.Invoke();
        TriggerFeedback(deathFeedback);
        AudioManager.Instance?.PlayOneShot(SoundIds.ShootEmUp.EnemyDeath);
        _behavior?.OnDeath();
    }

    private void SpawnEnergy()
    {
        if (data.energyCellPrefab == null) return;
        for (int i = 0; i < data.energyDropAmount; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.5f;
            Instantiate(data.energyCellPrefab, (Vector2)transform.position + offset, Quaternion.identity);
        }
    }

    /// <summary>Called by Animation Event at the end of a death animation.</summary>
    public void DestroyEnemy() => Destroy(gameObject);

    /// <summary>Safety fallback when no death anim event is configured.</summary>
    public void DestroyWithDelay(float delay = -1f)
    {
        float d = delay >= 0f ? delay : fallbackDestroyDelay;
        StartCoroutine(DestroyAfterDelay(d));
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    private static void TriggerFeedback(FeedbackConfigSO config)
    {
        if (config == null) return;
        HitStopManager.Instance?.FreezeFrame(config.hitStopDuration);
        if (config.cameraShake.magnitude > 0f)
            CameraShake.Instance?.Shake(config.cameraShake);
        CameraZoomFeedback.Instance?.Punch(config.zoomAmount, config.zoomDuration);
        VignetteFeedback.Instance?.Flash(config.vignetteIntensity, config.vignetteDuration);
    }
}
