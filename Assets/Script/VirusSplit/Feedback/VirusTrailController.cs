using UnityEngine;

/// <summary>
/// Manages the TrailRenderer on a virus GameObject.
/// Solves the visibility issue: a TrailRenderer with no material or a very long time
/// can overdraw the SpriteRenderer and make the sprite disappear.
///
/// This script:
///   - Forces the trail to use the assigned trailMaterial (URP Sprites-Default or a custom one).
///   - Keeps the trail time short so it does not cover the sprite.
///   - Provides Enable/Disable so VirusController can toggle the trail during split transitions.
///
/// Attach on the same GameObject as the TrailRenderer (VirusA and VirusB).
/// </summary>
[RequireComponent(typeof(TrailRenderer))]
public class VirusTrailController : MonoBehaviour
{
    [Header("Trail Settings")]
    [Tooltip("Material to use for the trail. Must be a transparent/additive URP Sprites material.")]
    [SerializeField] private Material trailMaterial;
    [Tooltip("How long (seconds) the trail persists. Keep short (0.15–0.4) to avoid covering the sprite.")]
    [SerializeField] private float trailTime          = 0.25f;
    [Tooltip("Width at the head of the trail (at the virus position).")]
    [SerializeField] private float startWidth         = 0.12f;
    [Tooltip("Width at the tail end of the trail.")]
    [SerializeField] private float endWidth           = 0f;
    [Tooltip("Color at the head of the trail.")]
    [SerializeField] private Color startColor         = new Color(1f, 1f, 1f, 0.8f);
    [Tooltip("Color at the tail (fully transparent).")]
    [SerializeField] private Color endColor           = new Color(1f, 1f, 1f, 0f);
    [Tooltip("Minimum vertex spacing. Lower = smoother but more vertices.")]
    [SerializeField] private float minVertexDistance  = 0.05f;

    private TrailRenderer _trail;

    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
        ApplySettings();
    }

    private void ApplySettings()
    {
        if (_trail == null) return;

        _trail.time              = trailTime;
        _trail.startWidth        = startWidth;
        _trail.endWidth          = endWidth;
        _trail.minVertexDistance = minVertexDistance;
        _trail.autodestruct      = false;

        // Assign sorting to match the Player layer so the trail renders behind the sprite.
        _trail.sortingLayerName = "Player";
        _trail.sortingOrder     = -1; // behind the SpriteRenderer (order 0)

        // Colour gradient.
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(startColor, 0f), new GradientColorKey(endColor, 1f) },
            new[] { new GradientAlphaKey(startColor.a, 0f), new GradientAlphaKey(0f, 1f) }
        );
        _trail.colorGradient = gradient;

        // Material — prevents the "null material makes sprite disappear" issue.
        if (trailMaterial != null)
            _trail.material = trailMaterial;

        _trail.emitting = true;
    }

    /// <summary>Clears all existing trail points and stops emitting.</summary>
    public void ClearTrail()
    {
        if (_trail == null) return;
        _trail.Clear();
        _trail.emitting = false;
    }

    /// <summary>Resumes emitting.</summary>
    public void ResumeTrail()
    {
        if (_trail == null) return;
        _trail.emitting = true;
    }

    private void OnValidate()
    {
        // Allow live-tweaking in the Inspector during Play mode.
        if (Application.isPlaying && _trail != null)
            ApplySettings();
    }
}
