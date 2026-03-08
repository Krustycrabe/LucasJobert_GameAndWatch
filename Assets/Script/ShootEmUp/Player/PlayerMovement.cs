using UnityEngine;

/// <summary>
/// Moves the player by the finger's screen delta (Aircaft-style).
/// The finger does not need to be on the character.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private float moveSensitivity = 1f;

    private Camera _mainCamera;
    private Vector2 _lastScreenPosition;
    private bool _isTracking;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        inputHandler.OnTouchBegan += HandleTouchBegan;
        inputHandler.OnTouchMoved += HandleTouchMoved;
        inputHandler.OnTouchEnded += HandleTouchEnded;
    }

    private void OnDisable()
    {
        inputHandler.OnTouchBegan -= HandleTouchBegan;
        inputHandler.OnTouchMoved -= HandleTouchMoved;
        inputHandler.OnTouchEnded -= HandleTouchEnded;
    }

    private void HandleTouchBegan(Vector2 screenPosition)
    {
        _lastScreenPosition = screenPosition;
        _isTracking = true;
    }

    private void HandleTouchMoved(Vector2 screenPosition)
    {
        if (!_isTracking) return;

        Vector2 screenDelta = screenPosition - _lastScreenPosition;
        _lastScreenPosition = screenPosition;

        // Convert screen pixel delta to world delta (works with orthographic camera)
        Vector3 worldOrigin = _mainCamera.ScreenToWorldPoint(Vector3.zero);
        Vector3 worldTarget = _mainCamera.ScreenToWorldPoint(new Vector3(screenDelta.x, screenDelta.y, 0f));
        Vector2 worldDelta = (worldTarget - worldOrigin) * moveSensitivity;

        Vector3 newPosition = transform.position + (Vector3)worldDelta;
        transform.position = ClampToCameraBounds(newPosition);
    }

    private void HandleTouchEnded()
    {
        _isTracking = false;
    }

    /// <summary>Clamps position within the visible camera bounds.</summary>
    private Vector3 ClampToCameraBounds(Vector3 position)
    {
        float halfHeight = _mainCamera.orthographicSize;
        float halfWidth = halfHeight * _mainCamera.aspect;
        Vector3 camPos = _mainCamera.transform.position;

        position.x = Mathf.Clamp(position.x, camPos.x - halfWidth, camPos.x + halfWidth);
        position.y = Mathf.Clamp(position.y, camPos.y - halfHeight, camPos.y + halfHeight);
        return position;
    }
}
