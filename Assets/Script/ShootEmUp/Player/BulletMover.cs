using UnityEngine;

/// <summary>
/// Moves the bullet to the right at constant speed. Self-destructs after lifetime.
/// </summary>
public class BulletMover : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(Vector2.right * (speed * Time.deltaTime));
    }
}
