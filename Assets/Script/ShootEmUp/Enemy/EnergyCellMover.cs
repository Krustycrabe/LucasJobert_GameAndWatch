using UnityEngine;

/// <summary>
/// Attracts the energy cell toward the player with accelerating speed.
/// On spawn, applies a random burst impulse (simulating the enemy death explosion).
/// The burst decays over burstDuration while the attraction already pulls the cell back.
/// Collected on trigger with the Player — adds energy to PlayerEnergyHandler.
/// </summary>
public class EnergyCellMover : MonoBehaviour
{
    [Header("Attraction")]
    [SerializeField] private float initialSpeed = 6f;
    [SerializeField] private float acceleration = 18f;

    [Header("Burst")]
    [Tooltip("Speed of the initial random ejection impulse.")]
    [SerializeField] private float burstSpeed = 5f;
    [Tooltip("Seconds over which the burst velocity decays to zero.")]
    [SerializeField] private float burstDuration = 0.35f;

    [Header("Collection")]
    [SerializeField] private int energyValue = 1;

    private Transform _playerTransform;
    private float _currentSpeed;
    private Vector2 _burstVelocity;
    private float _burstTimer;

    private void Start()
    {
        _currentSpeed = initialSpeed;
        CachePlayer();

        // Random burst direction in a full circle.
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        _burstVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * burstSpeed;
        _burstTimer = burstDuration;
    }

    private void Update()
    {
        CachePlayer();
        if (_playerTransform == null) return;

        // Decay the burst over time.
        Vector2 burst = Vector2.zero;
        if (_burstTimer > 0f)
        {
            _burstTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(_burstTimer / burstDuration);
            burst = _burstVelocity * t;
        }

        // Accelerating attraction.
        _currentSpeed += acceleration * Time.deltaTime;
        Vector2 toPlayer = ((Vector2)_playerTransform.position - (Vector2)transform.position).normalized;
        Vector2 attractionStep = toPlayer * _currentSpeed * Time.deltaTime;

        transform.position += (Vector3)(burst * Time.deltaTime + attractionStep);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        other.GetComponent<PlayerEnergyHandler>()?.CollectEnergy(energyValue);
        Destroy(gameObject);
    }

    private void CachePlayer()
    {
        if (_playerTransform != null) return;
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) _playerTransform = player.transform;
    }
}
