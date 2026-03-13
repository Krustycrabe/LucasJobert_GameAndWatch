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
    [SerializeField] private GameDataSO gameData;

    /// <summary>Fires with the new remaining life count whenever the player takes damage.</summary>
    public event Action<int> OnDamaged;

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
    /// Resets the score multiplier and starts invincibility frames.
    /// </summary>
    public void TakeDamage(int amount = 1)
    {
        if (_isInvincible || _currentLives <= 0) return;

        _currentLives = Mathf.Max(0, _currentLives - amount);
        _isInvincible = true;
        _invincibilityTimer = gameData.invincibilityDuration;

        SmUpScoreManager.Instance?.ResetMultiplier();
        OnDamaged?.Invoke(_currentLives);

        if (_currentLives == 0)
            OnDead?.Invoke();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Physical body contact with a living enemy.
        if (other.CompareTag("Enemy"))
        {
            EnemyCore core = other.GetComponentInParent<EnemyCore>();
            if (core != null && !core.IsDead)
                TakeDamage(1);
            return;
        }

        // Enemy projectile contact (Spear / generic EnemyBulletMover).
        if (other.GetComponent<EnemyBulletMover>() != null)
            TakeDamage(1);
    }
}
