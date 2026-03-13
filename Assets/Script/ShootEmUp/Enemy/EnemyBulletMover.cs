using System.Collections;
using UnityEngine;

/// <summary>
/// Moves an enemy projectile in a fixed world direction or along its own local forward (transform.right).
/// Use useLocalForward = true for aimed projectiles like the boss lance so the trajectory always
/// follows the object's orientation, even if an animation rotates it mid-flight.
/// On player contact, stops moving, disables the collider, plays the "Hit" animation clip,
/// then self-destructs — no damage is dealt during the hit animation.
/// Damage is handled exclusively by PlayerHealth.OnTriggerEnter2D via the IsSpear property.
/// </summary>
public class EnemyBulletMover : MonoBehaviour
{
    private static readonly int HitHash = Animator.StringToHash("Hit");

    [SerializeField] private float speed = 7f;
    [SerializeField] private float lifetime = 6f;
    [Tooltip("When true, the projectile moves along its own transform.right regardless of SetDirection.")]
    [SerializeField] private bool useLocalForward = false;
    [Tooltip("Flips the local forward axis. Enable if the projectile travels in the wrong direction.")]
    [SerializeField] private bool flipLocalForward = false;
    [Tooltip("Mark true on boss lance/spear projectiles. Used by PlayerHealth to route the correct feedback config.")]
    [SerializeField] private bool isSpear = false;
    [Tooltip("Animator that holds the Hit clip. Leave empty to skip the hit animation.")]
    [SerializeField] private Animator hitAnimator;
    [Tooltip("Duration of the Hit animation in seconds. The projectile is destroyed after this delay.")]
    [SerializeField] private float hitAnimDuration = 0.25f;

    /// <summary>True if this projectile is a spear (boss lance), false for standard bullets.</summary>
    public bool IsSpear => isSpear;

    private Vector2 _moveDirection = Vector2.left;
    private bool _hasHitPlayer;
    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (_hasHitPlayer) return;

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

        // Disable collider immediately so PlayerHealth.OnTriggerEnter2D
        // handles the damage once and no further triggers fire.
        if (_collider != null) _collider.enabled = false;

        if (hitAnimator != null)
            StartCoroutine(PlayHitAnimThenDestroy());
        else
            Destroy(gameObject);
    }

    /// <summary>Sets the travel direction for world-space projectiles. Ignored when useLocalForward is true.</summary>
    public void SetDirection(Vector2 direction)
    {
        _moveDirection = direction.normalized;
    }

    private IEnumerator PlayHitAnimThenDestroy()
    {
        hitAnimator.SetTrigger(HitHash);
        yield return new WaitForSeconds(hitAnimDuration);
        Destroy(gameObject);
    }
}

