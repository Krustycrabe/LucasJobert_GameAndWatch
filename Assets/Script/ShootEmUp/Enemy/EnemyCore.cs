using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Shared logic for all enemies: health, bullet collision, energy drop, death lifecycle.
/// Delegates per-frame behavior to the IEnemyBehavior component on the same GameObject.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnemyCore : MonoBehaviour
{
    [SerializeField] private EnemyDataSO data;
    [Tooltip("Fallback destroy delay when no death animation event is set up.")]
    [SerializeField] private float fallbackDestroyDelay = 0.5f;

    public event Action OnDeathEvent;

    public EnemyDataSO Data => data;
    public bool IsDead => _isDead;

    private int _currentHealth;
    private IEnemyBehavior _behavior;
    private bool _isDead;

    private void Awake()
    {
        _currentHealth = data.maxHealth;
        _behavior = GetComponent<IEnemyBehavior>();
        _behavior?.Initialize(this);
    }

    private void Update()
    {
        if (!_isDead) _behavior?.OnUpdate();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDead) return;
        if (!other.CompareTag("PlayerBullet")) return;

        BulletMover bullet = other.GetComponent<BulletMover>();
        int damage = bullet != null ? bullet.Damage : 1;
        Destroy(other.gameObject);
        TakeDamage(damage);
    }

    /// <summary>Reduces health by the given amount. Triggers death if health reaches zero.</summary>
    public void TakeDamage(int amount)
    {
        if (_isDead) return;
        _currentHealth -= amount;
        if (_currentHealth <= 0) Die();
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
        OnDeathEvent?.Invoke();
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
    public void DestroyEnemy()
    {
        Destroy(gameObject);
    }

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
}
