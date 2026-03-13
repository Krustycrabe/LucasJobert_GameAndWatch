using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Shared logic for all enemies: health, bullet collision, energy drop, death lifecycle.
/// Delegates per-frame behavior to the IEnemyBehavior component on the same GameObject.
/// Optionally plays a FeedbackConfigSO on hit and on death.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnemyCore : MonoBehaviour
{
    private static readonly int OnHitHash = Animator.StringToHash("OnHit");

    [SerializeField] private EnemyDataSO data;
    [Tooltip("Fallback destroy delay when no death animation event is set up.")]
    [SerializeField] private float fallbackDestroyDelay = 0.5f;

    [Header("VFX")]
    [Tooltip("Animator on the VFXHitLaser child GameObject. Receives the OnHit trigger on every hit.")]
    [SerializeField] private Animator vfxAnimator;

    [Header("Feedback")]
    [Tooltip("Feedback triggered each time this enemy takes a bullet hit. Leave empty to skip.")]
    [SerializeField] private FeedbackConfigSO hitFeedback;
    [Tooltip("Feedback triggered when this enemy dies. Leave empty to skip.")]
    [SerializeField] private FeedbackConfigSO deathFeedback;

    public event Action OnDeathEvent;

    public EnemyDataSO Data => data;
    public bool IsDead => _isDead;

    private int _currentHealth;
    private IEnemyBehavior _behavior;
    private bool _isDead;
    private bool _keepUpdatingAfterDeath;

    private void Awake()
    {
        _currentHealth = data.maxHealth;
        _behavior = GetComponent<IEnemyBehavior>();
        _behavior?.Initialize(this);
    }

    private void OnEnable()  => GameOverEvents.OnGameOver += HandleGameOver;
    private void OnDisable() => GameOverEvents.OnGameOver -= HandleGameOver;

    private void Update()
    {
        if (!_isDead || _keepUpdatingAfterDeath) _behavior?.OnUpdate();
    }

    /// <summary>
    /// Allows a behavior to keep receiving OnUpdate() calls even after the EnemyCore is marked dead.
    /// Used by KamikazeBehavior to continue moving during the explosion animation.
    /// </summary>
    public void SetKeepUpdatingAfterDeath(bool value) => _keepUpdatingAfterDeath = value;

    /// <summary>Forces death animation on all living enemies when the game is over.</summary>
    private void HandleGameOver()
    {
        if (_isDead) return;
        _isDead = true;
        _behavior?.OnDeath();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDead) return;
        if (!other.CompareTag("PlayerBullet")) return;

        BulletMover bullet = other.GetComponent<BulletMover>();
        if (bullet == null || bullet.HasHit) return;

        int damage = bullet.Damage;
        bullet.TriggerHit();
        TakeDamage(damage);
    }

    /// <summary>
    /// Reduces health by the given amount from a standard bullet hit.
    /// Triggers hitFeedback on non-lethal hits.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (_isDead) return;
        _currentHealth -= amount;

        if (_currentHealth <= 0)
        {
            Die();
        }
        else
        {
            TriggerFeedback(hitFeedback);
        }
    }

    /// <summary>
    /// Reduces health by the given amount from a laser tick.
    /// Plays the OnHit animator trigger without the hitFeedback SO —
    /// laser feedback is handled per-tick by LaserHitbox using its own FeedbackConfigSO.
    /// </summary>
    public void TakeLaserDamage(int amount)
    {
        if (_isDead) return;
        _currentHealth -= amount;

        vfxAnimator?.SetTrigger(OnHitHash);

        if (_currentHealth <= 0)
            Die();
    }

    /// <summary>Forces death regardless of current health. Used by Kamikaze on explosion.</summary>
    public void ForceKill()
    {
        if (_isDead) return;
        Die();
    }

    private void Die()
    {
        _isDead = true;
        SpawnEnergy();
        SmUpScoreManager.Instance?.AwardKillScore(data.scoreValue);
        OnDeathEvent?.Invoke();
        TriggerFeedback(deathFeedback);
        _behavior?.OnDeath();
    }

    private void SpawnEnergy()
    {
        if (data.energyCellPrefab == null) return;
        for (int i = 0; i < data.energyDropAmount; i++)
        {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * 0.5f;
            Instantiate(data.energyCellPrefab, (Vector2)transform.position + randomOffset, Quaternion.identity);
        }
    }

    /// <summary>Called by Animation Event at the end of a death or explosion animation.</summary>
    public void DestroyEnemy() => Destroy(gameObject);

    /// <summary>Destroys after a delay. Used as fallback when no death anim event is configured.</summary>
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
