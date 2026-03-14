using UnityEngine;

/// <summary>
/// Infinite horizontal parallax layer.
/// Requires exactly two child GameObjects (Slot_A and Slot_B) each with a SpriteRenderer
/// showing the same sprite. At runtime the two slots are positioned side by side and scroll
/// left continuously; the one that leaves the screen is recycled to the right of the other.
/// </summary>
public class ParallaxScroller : MonoBehaviour
{
    [Tooltip("Scroll speed in world units per second. Positive = scrolls left.")]
    [SerializeField] private float scrollSpeed = 3f;

    private Transform _slotA;
    private Transform _slotB;
    private float     _spriteWidth;
    private float     _camLeftEdge;

    // Cached half-width: camera left = cam.x - halfWidth
    private float _camHalfWidth;
    private Transform _camTransform;

    /// <summary>Exposes scroll speed so a manager can scale it at runtime.</summary>
    public float ScrollSpeed
    {
        get => scrollSpeed;
        set => scrollSpeed = value;
    }

    private void Start()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            _camTransform = cam.transform;
            _camHalfWidth = cam.orthographicSize * cam.aspect;
        }

        if (transform.childCount < 2)
        {
            Debug.LogError($"ParallaxScroller on '{name}' requires exactly 2 child GameObjects (Slot_A and Slot_B).", this);
            enabled = false;
            return;
        }

        _slotA = transform.GetChild(0);
        _slotB = transform.GetChild(1);

        SpriteRenderer rend = _slotA.GetComponent<SpriteRenderer>();
        if (rend == null || rend.sprite == null)
        {
            Debug.LogError($"ParallaxScroller: Slot_A on '{name}' is missing a SpriteRenderer or sprite.", this);
            enabled = false;
            return;
        }

        _spriteWidth = rend.bounds.size.x;

        Vector3 origin = transform.position;
        _slotA.position = origin;
        _slotB.position = new Vector3(origin.x + _spriteWidth, origin.y, origin.z);
    }

    private void Update()
    {
        float delta = scrollSpeed * Time.deltaTime;

        _slotA.Translate(Vector3.left * delta, Space.World);
        _slotB.Translate(Vector3.left * delta, Space.World);

        // Recompute left edge only when camera moves (orthographic games rarely do).
        float camLeft = _camTransform != null
            ? _camTransform.position.x - _camHalfWidth
            : -20f;

        RecycleIfOffScreen(_slotA, _slotB, camLeft);
        RecycleIfOffScreen(_slotB, _slotA, camLeft);
    }

    private void RecycleIfOffScreen(Transform target, Transform anchor, float camLeft)
    {
        if (target.position.x + _spriteWidth * 0.5f < camLeft)
        {
            target.position = new Vector3(
                anchor.position.x + _spriteWidth,
                target.position.y,
                target.position.z);
        }
    }
}
