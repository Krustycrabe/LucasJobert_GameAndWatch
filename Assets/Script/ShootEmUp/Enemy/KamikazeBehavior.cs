using UnityEngine;

/// <summary>
/// Kamikaze enemy: charges toward the player. When in range, triggers explosion animation.
/// If killed by bullet before reaching detection range, falls back to immediate destroy.
/// Animation Events on BoomExplode.anim must call DestroyEnemy() on EnemyCore at last frame.
/// </summary>
[RequireComponent(typeof(EnemyCore))]
[RequireComponent(typeof(Animator))]
public class KamikazeBehavior : MonoBehaviour, IEnemyBehavior
{
    [SerializeField] private float detectionRange = 4f;
    [SerializeField] private float prepSpeedMultiplier = 0.35f;

    private static readonly int StartExplodeHash = Animator.StringToHash("StartExplode");

    private EnemyCore _core;
    private Animator _animator;
    private Transform _playerTransform;
    private bool _isPrepping;

    public void Initialize(EnemyCore core)
    {
        _core = core;
        _animator = GetComponent<Animator>();
    }

    public void OnUpdate()
    {
        CachePlayer();
        if (_playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, _playerTransform.position);

        if (!_isPrepping && distance <= detectionRange)
        {
            _isPrepping = true;
            _animator.SetTrigger(StartExplodeHash);
            _core.ForceKill();
        }

        float speed = _isPrepping
            ? _core.Data.moveSpeed * prepSpeedMultiplier
            : _core.Data.moveSpeed;

        transform.position = Vector2.MoveTowards(
            transform.position,
            _playerTransform.position,
            speed * Time.deltaTime);
    }

    public void OnDeath()
    {
        // Killed by bullet before reaching the player: no explosion anim, fallback destroy.
        if (!_isPrepping)
            _core.DestroyWithDelay();
        // Killed by ForceKill (reached player): BoomExplode anim plays,
        // DestroyEnemy() fires via Animation Event at last frame.
    }

    /// <summary>
    /// Called by Animation Event at the peak explosion frame.
    /// Player collision script handles damage reception.
    /// </summary>
    public void OnExplosionFrame()
    {
        // Reserved for player-side collision handling.
    }

    private void CachePlayer()
    {
        if (_playerTransform != null) return;
        var player = GameObject.FindWithTag("Player");
        if (player != null) _playerTransform = player.transform;
    }
}
