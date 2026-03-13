using UnityEngine;

/// <summary>
/// Shooter enemy: enters screen fast, wanders randomly in the right zone, periodically charges the
/// player, avoids other enemies via separation, and shoots at a fixed rate.
/// </summary>
[RequireComponent(typeof(EnemyCore))]
[RequireComponent(typeof(Animator))]
public class EnemyShooterBehavior : MonoBehaviour, IEnemyBehavior
{
    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;

    [Header("Movement")]
    [Tooltip("Speed multiplier applied only during the screen-entry phase.")]
    [SerializeField] private float entrySpeedMultiplier = 4f;
    [Tooltip("X position the enemy slides to after spawning.")]
    [SerializeField] private float engagePositionX = 5f;
    [Tooltip("Leftmost X position used for wander target picking.")]
    [SerializeField] private float minWanderX = 1.5f;
    [SerializeField] private float edgePadding = 0.5f;
    [SerializeField] private float wanderReachThreshold = 0.25f;

    [Header("Chase")]
    [Tooltip("Seconds between each charge toward the player.")]
    [SerializeField] private float chaseInterval = 5f;
    [Tooltip("Duration of each charge phase.")]
    [SerializeField] private float chaseDuration = 2f;
    [Tooltip("Speed multiplier during the charge phase.")]
    [SerializeField] private float chaseSpeedMultiplier = 1.8f;

    [Header("Separation")]
    [SerializeField] private float separationRadius = 1.2f;
    [SerializeField] private float separationStrength = 3f;

    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int DeadHash  = Animator.StringToHash("Dead");

    private EnemyCore _core;
    private Animator _animator;
    private Transform _playerTransform;
    private Camera _mainCamera;
    private bool _hasEntered;

    private Vector2 _wanderTarget;
    private float _chaseTimer;
    private float _chaseDurationTimer;
    private bool _isChasing;
    private float _shootTimer;

    private readonly Collider2D[] _separationBuffer = new Collider2D[8];

    public void Initialize(EnemyCore core)
    {
        _core = core;
        _animator = GetComponent<Animator>();
        _mainCamera = Camera.main;
        _hasEntered = false;
        // Stagger chase start so all enemies don't charge at the same time.
        _chaseTimer = Random.Range(0f, chaseInterval * 0.5f);
    }

    public void OnUpdate()
    {
        CachePlayer();

        if (!_hasEntered) { EnterScreen(); return; }

        UpdateChaseTimer();

        if (_isChasing) Chase();
        else Wander();

        ApplySeparation();
        HandleShooting();
    }

    public void OnDeath()
    {
        _animator.SetTrigger(DeadHash);
        _core.DestroyWithDelay(1.5f);
        CameraShake.Instance?.Shake(CameraShake.Instance.EnemyDeathShake);
    }

    /// <summary>Called by Animation Event at the shoot frame of BrainShoot.anim.</summary>
    public void SpawnBullet()
    {
        if (bulletPrefab == null) return;
        Transform origin = bulletSpawnPoint != null ? bulletSpawnPoint : transform;
        Instantiate(bulletPrefab, origin.position, Quaternion.identity);
    }

    private void EnterScreen()
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            new Vector2(engagePositionX, transform.position.y),
            _core.Data.moveSpeed * entrySpeedMultiplier * Time.deltaTime);

        if (transform.position.x <= engagePositionX)
        {
            _hasEntered = true;
            PickNewWanderTarget();
        }
    }

    private void Wander()
    {
        transform.position = Vector2.MoveTowards(
            transform.position, _wanderTarget,
            _core.Data.moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, _wanderTarget) < wanderReachThreshold)
            PickNewWanderTarget();
    }

    private void Chase()
    {
        if (_playerTransform == null) return;
        transform.position = Vector2.MoveTowards(
            transform.position, _playerTransform.position,
            _core.Data.moveSpeed * chaseSpeedMultiplier * Time.deltaTime);
    }

    private void UpdateChaseTimer()
    {
        if (_isChasing)
        {
            _chaseDurationTimer += Time.deltaTime;
            if (_chaseDurationTimer >= chaseDuration)
            {
                _isChasing = false;
                _chaseDurationTimer = 0f;
                PickNewWanderTarget();
            }
        }
        else
        {
            _chaseTimer += Time.deltaTime;
            if (_chaseTimer >= chaseInterval)
            {
                _chaseTimer = 0f;
                _isChasing = true;
            }
        }
    }

    private void ApplySeparation()
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, separationRadius, _separationBuffer);
        Vector2 push = Vector2.zero;
        for (int i = 0; i < count; i++)
        {
            Collider2D col = _separationBuffer[i];
            if (col == null || col.gameObject == gameObject || !col.CompareTag("Enemy")) continue;
            Vector2 away = (Vector2)(transform.position - col.transform.position);
            float dist = away.magnitude;
            if (dist > 0f && dist < separationRadius)
                push += away.normalized * (1f - dist / separationRadius);
        }
        if (push != Vector2.zero)
            transform.position = (Vector2)transform.position + push * separationStrength * Time.deltaTime;
    }

    private void HandleShooting()
    {
        _shootTimer += Time.deltaTime;
        if (_shootTimer >= _core.Data.shootRate)
        {
            _shootTimer = 0f;
            _animator.SetTrigger(ShootHash);
        }
    }

    private void PickNewWanderTarget()
    {
        float camH = _mainCamera != null ? _mainCamera.orthographicSize : 5f;
        float camRight = _mainCamera != null
            ? _mainCamera.transform.position.x + camH * _mainCamera.aspect
            : 8f;

        float x = Random.Range(minWanderX, Mathf.Min(engagePositionX, camRight - edgePadding));
        float y = Random.Range(-camH + edgePadding, camH - edgePadding);
        _wanderTarget = new Vector2(x, y);
    }

    private void CachePlayer()
    {
        if (_playerTransform != null) return;
        var player = GameObject.FindWithTag("Player");
        if (player != null) _playerTransform = player.transform;
    }
}

