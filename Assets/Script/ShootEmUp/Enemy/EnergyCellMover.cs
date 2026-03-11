using UnityEngine;

/// <summary>
/// Attracts the energy cell toward the player with accelerating speed.
/// Collected on collision with the Player, adds energy to PlayerEnergyHandler.
/// </summary>
public class EnergyCellMover : MonoBehaviour
{
    [SerializeField] private float initialSpeed = 3f;
    [SerializeField] private float acceleration = 4f;
    [SerializeField] private int energyValue = 1;

    private Transform _playerTransform;
    private float _currentSpeed;

    private void Start()
    {
        _currentSpeed = initialSpeed;
        CachePlayer();
    }

    private void Update()
    {
        CachePlayer();
        if (_playerTransform == null) return;

        _currentSpeed += acceleration * Time.deltaTime;
        transform.position = Vector2.MoveTowards(
            transform.position,
            _playerTransform.position,
            _currentSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerEnergyHandler energyHandler = other.GetComponent<PlayerEnergyHandler>();
        energyHandler?.CollectEnergy(energyValue);
        Destroy(gameObject);
    }

    private void CachePlayer()
    {
        if (_playerTransform != null) return;
        var player = GameObject.FindWithTag("Player");
        if (player != null) _playerTransform = player.transform;
    }
}
