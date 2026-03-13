using UnityEngine;

/// <summary>
/// ScriptableObject that describes the complete visual and temporal feedback
/// for a single game event (player hit, laser tick, kamikaze explosion, etc.).
/// Create one asset per event type and assign it to the component that needs it.
/// </summary>
[CreateAssetMenu(fileName = "FeedbackConfig", menuName = "ShootEmUp/Feedback Config")]
public class FeedbackConfigSO : ScriptableObject
{
    [Header("Hit Stop")]
    [Tooltip("Duration of the time freeze in seconds (unscaled). 0 = disabled.")]
    public float hitStopDuration = 0.06f;

    [Header("Camera Shake")]
    [Tooltip("Leave magnitude at 0 to skip shake.")]
    public ShakeData cameraShake = new ShakeData(0.08f, 0.20f);

    [Header("Camera Zoom")]
    [Tooltip("Orthographic size delta applied at hit peak. Negative = zoom in. 0 = disabled.")]
    public float zoomAmount = -0.15f;
    [Tooltip("Duration of the zoom-in then zoom-out in seconds (unscaled).")]
    public float zoomDuration = 0.12f;

    [Header("Vignette")]
    [Range(0f, 1f)]
    [Tooltip("Target vignette intensity at peak. 0 = disabled.")]
    public float vignetteIntensity = 0.45f;
    [Tooltip("Duration of the vignette flash in seconds (unscaled).")]
    public float vignetteDuration = 0.18f;
}
