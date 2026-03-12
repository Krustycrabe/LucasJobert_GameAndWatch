using System.Collections;
using UnityEngine;

/// <summary>
/// Moves the bullet to the right at constant speed. Self-destructs after lifetime or on exiting camera bounds.
/// On hit, triggers the Hit animation, freezes movement and damage, then self-destructs after the anim plays.
/// </summary>
public class BulletMover : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private int damage = 1;
    [Tooltip("How long to wait after triggering Hit before destroying. Should match the hit animation length.")]
    [SerializeField] private float hitAnimDuration = 0.3f;

    private static readonly int HitHash = Animator.StringToHash("Hit");

    public int Damage => damage;
    public bool HasHit => _hasHit;

    private Camera _mainCamera;
    private Animator _animator;
    private bool _hasHit;

    private void Start()
    {
        _mainCamera = Camera.main;
        _animator = GetComponent<Animator>();
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (_hasHit) return;
        transform.Translate(Vector2.right * (speed * Time.deltaTime));
        DestroyIfOutOfBounds();
    }

    /// <summary>
    /// Called by EnemyCore on collision. Freezes the bullet, plays the hit animation,
    /// then destroys after hitAnimDuration. Damage is locked after the first call.
    /// </summary>
    public void TriggerHit()
    {
        if (_hasHit) return;
        _hasHit = true;

        CancelInvoke();
        StopAllCoroutines();

        if (_animator != null)
            _animator.SetTrigger(HitHash);

        StartCoroutine(DestroyAfterHitAnim());
    }

    /// <summary>Called by Animation Event on the last frame of the hit animation (optional).</summary>
    public void DestroyBullet() => Destroy(gameObject);

    private IEnumerator DestroyAfterHitAnim()
    {
        yield return new WaitForSeconds(hitAnimDuration);
        Destroy(gameObject);
    }

    private void DestroyIfOutOfBounds()
    {
        if (_mainCamera == null) return;
        Vector3 vp = _mainCamera.WorldToViewportPoint(transform.position);
        if (vp.x > 1.1f || vp.x < -0.1f || vp.y > 1.1f || vp.y < -0.1f)
            Destroy(gameObject);
    }
}
