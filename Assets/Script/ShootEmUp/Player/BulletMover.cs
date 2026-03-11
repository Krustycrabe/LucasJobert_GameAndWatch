using UnityEngine;

/// <summary>
/// Moves the bullet to the right at constant speed. Self-destructs after lifetime or on exiting camera bounds.
/// Exposes Damage for EnemyCore to read on collision.
/// </summary>
public class BulletMover : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private int damage = 1;

    public int Damage => damage;

    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(Vector2.right * (speed * Time.deltaTime));
        DestroyIfOutOfBounds();
    }

    /// <summary>Destroys the bullet as soon as it leaves the visible camera area.</summary>
    private void DestroyIfOutOfBounds()
    {
        if (_mainCamera == null) return;
        Vector3 vp = _mainCamera.WorldToViewportPoint(transform.position);
        if (vp.x > 1.1f || vp.x < -0.1f || vp.y > 1.1f || vp.y < -0.1f)
            Destroy(gameObject);
    }
}
