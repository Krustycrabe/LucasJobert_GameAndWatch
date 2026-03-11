using UnityEngine;

/// <summary>
/// Boss enemy: enters screen fast, wanders in the right zone, periodically charges the player,
/// avoids other enemies, aims and throws a lance at a fixed rate.
/// </summary>
[RequireComponent(typeof(EnemyCore))]
[RequireComponent(typeof(Animator))]
public class BossBehavior : MonoBehaviour, IEnemyBehavior
{
    [Header("Lance")]
    [SerializeField] private GameObject lancePrefab;
    [SerializeField] private Transform lanceSpawnPoint;
    [SerializeField] private float aimRotationSpeed = 360f;
    [Tooltip("Angle offset (degrees) to match the sprite's default orientation. 180 if sprite faces left.")]
    [SerializeField] private float aimAngleOffset = 180f;

    [Header("Movement")]
    [Tooltip("Speed multiplier applied only during the screen-entry phase.")]
    [SerializeField] private float entrySpeedMultiplier = 4f;
    [Tooltip("X position the boss slides to after spawning.")]
    [SerializeField] private float engagePositionX = 6f;
    [Tooltip("Leftmost X position used for wander target picking.")]
    [SerializeField] private float minWanderX = 3f;
    [SerializeField] private float edgePadding = 0.5f;
    [SerializeField] private float wanderReachThreshold = 0.3f;

    [Header("Chase")]
    [Tooltip("Seconds between each charge toward the player.")]
    [SerializeField] private float chaseInterval = 6f;
    [Tooltip("Duration of each charge phase.")]
    [SerializeField] private float chaseDuration = 2.5f;
    [Tooltip("Speed multiplier during the charge phase.")]
    [SerializeField] private float chaseSpeedMultiplier = 2f;

    [Header("Separation")]
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float separationStrength = 3f;

    private static readonly int StartThrowHash = Animator.StringToHash("StartThrow");

    private EnemyCore _core;
    private Animator _animator;
    private Transform _playerTransform;
    private Camera _mainCamera;
    private bool _hasEntered;

    private Vector2 _wanderTarget;
    private float _chaseTimer;
    private float _chaseDurationTimer;
    private bool _isChasing;
    private bool _isAiming;
    private float _throwTimer;

    private readonly Collider2D[] _separationBuffer = new Collider2D[8];

    public void Initialize(EnemyCore core)
    {
        _core = core;
        _animator = GetComponent<Animator>();
        _mainCamera = Camera.main;
        _hasEntered = false;
        _chaseTimer = Random.Range(0f, chaseInterval * 0.5f);
    }

    public void OnUpdate()
    {
        CachePlayer();

        if (!_hasEntered) { EnterScreen(); return; }

        UpdateChaseTimer();
        Aim();

        if (!_isAiming)
        {
            if (_isChasing) Chase();
            else Wander();
            ApplySeparation();
        }

        // Only accumulate throw timer once the boss is in play, not during entry.
        _throwTimer += Time.deltaTime;
        if (!_isAiming && _throwTimer >= _core.Data.shootRate)
        {
            _throwTimer = 0f;
            StartThrow();
        }
    }

    public void OnDeath()
    {
        _isAiming = false;
        _core.DestroyWithDelay();
    }

    /// <summary>Called by Animation Event at the release frame of GigaChad_Throw.anim.</summary>
    public void SpawnLance()
    {
        if (lancePrefab == null) return;
        Transform origin = lanceSpawnPoint != null ? lanceSpawnPoint : transform;
        Vector2 direction = _playerTransform != null
            ? ((Vector2)_playerTransform.position - (Vector2)origin.position).normalized
            : Vector2.left;

        var lance = Instantiate(lancePrefab, origin.position, transform.rotation);
        lance.GetComponent<EnemyBulletMover>()?.SetDirection(direction);
        _isAiming = false;
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
            _throwTimer = 0f; // reset so the first throw fires after a full shootRate delay
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

    private void Aim()
    {
        if (!_isAiming || _playerTransform == null) return;
        Vector2 direction = (Vector2)_playerTransform.position - (Vector2)transform.position;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + aimAngleOffset;
        float newAngle = Mathf.MoveTowardsAngle(transform.eulerAngles.z, targetAngle, aimRotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
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

    private void StartThrow()
    {
        _isAiming = true;
        _animator.SetTrigger(StartThrowHash);
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
