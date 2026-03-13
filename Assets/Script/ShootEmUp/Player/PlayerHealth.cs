using System;
using UnityEngine;

/// <summary>
/// Manages player lives and handles all sources of incoming damage:
/// physical enemy contact, enemy projectiles (EnemyBulletMover), and explicit calls
/// (e.g. from Kamikaze explosion overlap check).
/// Applies invincibility frames after every hit to prevent rapid repeated damage.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    /// <summary>Categorizes the source of damage for feedback routing.</summary>
    public enum DamageSource { Contact, Bullet, Spear, Explosion }

    [SerializeField] private GameDataSO gameData;

    /// <summary>Fires with the remaining life count and damage source on each hit.</summary>
    public event Action<int, DamageSource> OnDamaged;

    /// <summary>Fires when lives reach zero.</summary>
    public event Action OnDead;

    public int CurrentLives => _currentLives;
    public bool IsInvincible => _isInvincible;

    private int _currentLives;
    private bool _isInvincible;
    private float _invincibilityTimer;

    private void Awake()
    {
        _currentLives = gameData.maxLives;
    }

    private void Update()
    {
        if (!_isInvincible) return;
        _invincibilityTimer -= Time.deltaTime;
        if (_invincibilityTimer <= 0f)
            _isInvincible = false;
    }

    /// <summary>
    /// Inflicts damage on the player.
    /// Ignored if the player is currently invincible or already dead.
    /// Resets the score multiplier. Invincibility frames are only applied when the
    /// player survives the hit (avoids blocking the death event on the final life).
    /// </summary>
    public void TakeDamage(int amount = 1, DamageSource source = DamageSource.Contact)
    {
        if (_isInvincible || _currentLives <= 0) return;

        _currentLives = Mathf.Max(0, _currentLives - amount);

        SmUpScoreManager.Instance?.ResetMultiplier();
        OnDamaged?.Invoke(_currentLives, source);

        if (_currentLives == 0)
        {
            OnDead?.Invoke();
            return;
        }

        _isInvincible = true;
        _invincibilityTimer = gameData.invincibilityDuration;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Enemy projectile contact — check for spear (boss) vs bullet (brain).
        EnemyBulletMover bullet = other.GetComponent<EnemyBulletMover>();
        if (bullet != null)
        {
            DamageSource source = bullet.IsSpear ? DamageSource.Spear : DamageSource.Bullet;
            TakeDamage(1, source);
            return;
        }

        // Physical body contact with a living enemy.
        // Kamikazes in prep phase deal damage only via OnExplosionFrame, not physical contact.
        if (other.CompareTag("Enemy"))
        {
            EnemyCore core = other.GetComponentInParent<EnemyCore>();
            if (core == null || core.IsDead) return;

            KamikazeBehavior kamikaze = other.GetComponentInParent<KamikazeBehavior>();
            if (kamikaze != null && kamikaze.IsPrepping) return;

            TakeDamage(1, DamageSource.Contact);
        }
    }
}
