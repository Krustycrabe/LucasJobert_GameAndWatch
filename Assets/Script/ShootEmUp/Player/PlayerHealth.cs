using System;
using UnityEngine;

/// <summary>
/// Manages player lives and handles all incoming damage sources.
/// Invincibility frames prevent rapid repeated damage.
/// The explosion collider of the Kamikaze uses the standard "Enemy" contact path —
/// no special cases needed here since EnemyCore.BeginExplosion() disables the body
/// collider and uses a separate growing CircleCollider2D for the blast radius.
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

    public int  CurrentLives  => _currentLives;
    public bool IsInvincible  => _isInvincible;

    private int   _currentLives;
    private bool  _isInvincible;
    private float _invincibilityTimer;
    private bool  _isDead;

    private void Awake()
    {
        _currentLives = gameData.maxLives;
    }

    private void Update()
    {
        if (!_isInvincible) return;
        _invincibilityTimer -= Time.deltaTime;
        if (_invincibilityTimer <= 0f) _isInvincible = false;
    }

    /// <summary>
    /// Inflicts damage respecting invincibility frames.
    /// Ignored if the player is invincible or already dead.
    /// </summary>
    public void TakeDamage(int amount = 1, DamageSource source = DamageSource.Contact)
    {
        if (_isDead || _isInvincible) return;
        ApplyDamage(amount, source);
    }

    private void ApplyDamage(int amount, DamageSource source)
    {
        _currentLives = Mathf.Max(0, _currentLives - amount);

        SmUpScoreManager.Instance?.ResetMultiplier();
        OnDamaged?.Invoke(_currentLives, source);

        if (_currentLives == 0)
        {
            _isDead = true;
            OnDead?.Invoke();
            return;
        }

        _isInvincible       = true;
        _invincibilityTimer = gameData.invincibilityDuration;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Enemy projectile — bullet or spear.
        EnemyBulletMover bullet = other.GetComponent<EnemyBulletMover>();
        if (bullet != null)
        {
            TakeDamage(1, bullet.IsSpear ? DamageSource.Spear : DamageSource.Bullet);
            return;
        }

        // Physical contact with a living enemy (includes the Kamikaze explosion CircleCollider2D).
        // EnemyCore.BeginExplosion() already disabled the body collider, so only the
        // growing explosion zone can reach here during the explosion animation.
        if (other.CompareTag("Enemy"))
        {
            EnemyCore core = other.GetComponentInParent<EnemyCore>();
            if (core == null || core.IsDead) return;

            // Explosion zone contact uses DamageSource.Explosion for feedback routing.
            DamageSource src = core.IsExploding ? DamageSource.Explosion : DamageSource.Contact;
            TakeDamage(1, src);
        }
    }
}

