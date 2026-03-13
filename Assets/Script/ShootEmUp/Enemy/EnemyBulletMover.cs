using UnityEngine;

/// <summary>
/// Moves an enemy projectile in a fixed world direction or along its own local forward (transform.right).
/// Use useLocalForward = true for aimed projectiles like the boss lance so the trajectory always
/// follows the object's orientation, even if an animation rotates it mid-flight.
/// Deals 1 damage to the player on contact (requires the projectile's Collider2D to be a trigger).
/// </summary>
public class EnemyBulletMover : MonoBehaviour
{
    [SerializeField] private float speed = 7f;
    [SerializeField] private float lifetime = 6f;
    [Tooltip("When true, the projectile moves along its own transform.right regardless of SetDirection.")]
    [SerializeField] private bool useLocalForward = false;
    [Tooltip("Flips the local forward axis. Enable if the projectile travels in the wrong direction.")]
    [SerializeField] private bool flipLocalForward = false;

    private Vector2 _moveDirection = Vector2.left;
    private bool _hasHitPlayer;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        Vector2 direction = useLocalForward
            ? (Vector2)transform.right * (flipLocalForward ? -1f : 1f)
            : _moveDirection;
        transform.Translate(direction * (speed * Time.deltaTime), Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHitPlayer) return;
        if (!other.CompareTag("Player")) return;

        _hasHitPlayer = true;
        other.GetComponent<PlayerHealth>()?.TakeDamage(1);
        Destroy(gameObject);
    }

    /// <summary>Sets the travel direction for world-space projectiles. Ignored when useLocalForward is true.</summary>
    public void SetDirection(Vector2 direction)
    {
        _moveDirection = direction.normalized;
    }
}

