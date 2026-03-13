using System.Collections;
using UnityEngine;

/// <summary>
/// Applies a brief one-shot zoom punch to the main camera's orthographic size.
/// Attach to the Main Camera alongside CameraShake.
/// Call Punch() from any feedback orchestrator.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraZoomFeedback : MonoBehaviour
{
    public static CameraZoomFeedback Instance { get; private set; }

    private Camera _camera;
    private float _baseSize;
    private Coroutine _currentPunch;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        _camera = GetComponent<Camera>();
        _baseSize = _camera.orthographicSize;
    }

    /// <summary>
    /// Punches the camera zoom by <paramref name="amount"/> over <paramref name="duration"/> seconds (unscaled).
    /// Negative amount = zoom in, positive = zoom out.
    /// </summary>
    public void Punch(float amount, float duration)
    {
        if (Mathf.Approximately(amount, 0f) || duration <= 0f) return;

        if (_currentPunch != null) StopCoroutine(_currentPunch);
        _currentPunch = StartCoroutine(PunchRoutine(amount, duration));
    }

    private IEnumerator PunchRoutine(float amount, float duration)
    {
        float halfDuration = duration * 0.5f;
        float elapsed = 0f;

        // Zoom in
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
            _camera.orthographicSize = _baseSize + amount * t;
            yield return null;
        }

        elapsed = 0f;

        // Zoom out back to base
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
            _camera.orthographicSize = (_baseSize + amount) + (-amount) * t;
            yield return null;
        }

        _camera.orthographicSize = _baseSize;
        _currentPunch = null;
    }
}
