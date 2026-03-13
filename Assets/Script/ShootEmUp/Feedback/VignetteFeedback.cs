using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Drives a URP Vignette post-process override to flash on impactful events.
/// Attach to the same GameObject as the global Volume that holds the Vignette override.
/// The Vignette override must exist in the Volume profile with "Intensity" set to override.
/// Call Flash() from any feedback orchestrator.
/// </summary>
[RequireComponent(typeof(Volume))]
public class VignetteFeedback : MonoBehaviour
{
    public static VignetteFeedback Instance { get; private set; }

    private Vignette _vignette;
    private float _baseIntensity;
    private Coroutine _currentFlash;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        Volume volume = GetComponent<Volume>();
        if (!volume.profile.TryGet(out _vignette))
        {
            Debug.LogWarning("[VignetteFeedback] No Vignette override found in Volume profile.", this);
            enabled = false;
            return;
        }

        _vignette.intensity.overrideState = true;
        _baseIntensity = _vignette.intensity.value;
    }

    /// <summary>
    /// Flashes the vignette to <paramref name="targetIntensity"/> then fades back
    /// over <paramref name="duration"/> seconds (unscaled).
    /// </summary>
    public void Flash(float targetIntensity, float duration)
    {
        if (_vignette == null || Mathf.Approximately(targetIntensity, 0f) || duration <= 0f) return;

        if (_currentFlash != null) StopCoroutine(_currentFlash);
        _currentFlash = StartCoroutine(FlashRoutine(targetIntensity, duration));
    }

    private IEnumerator FlashRoutine(float targetIntensity, float duration)
    {
        float elapsed = 0f;

        // Flash in (25% of duration) then fade out (75%)
        float attackTime = duration * 0.25f;
        float releaseTime = duration * 0.75f;

        while (elapsed < attackTime)
        {
            elapsed += Time.unscaledDeltaTime;
            _vignette.intensity.value = Mathf.Lerp(_baseIntensity, targetIntensity, elapsed / attackTime);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < releaseTime)
        {
            elapsed += Time.unscaledDeltaTime;
            _vignette.intensity.value = Mathf.Lerp(targetIntensity, _baseIntensity, elapsed / releaseTime);
            yield return null;
        }

        _vignette.intensity.value = _baseIntensity;
        _currentFlash = null;
    }
}
