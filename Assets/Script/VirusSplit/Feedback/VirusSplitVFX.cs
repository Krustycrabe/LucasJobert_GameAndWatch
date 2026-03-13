using System.Collections;
using UnityEngine;

/// <summary>
/// Manages all VFX for the VirusSplit mini-game:
///   - Scale punch on tap (both viruses)
///   - Split particle burst at the split origin
///   - Merge circle shrinking to the merge destination
///   - Merge arrival VFX on the player
///
/// Attach to a dedicated VFX GameObject in the scene.
/// Particle systems and the merge circle are assigned in the Inspector.
/// </summary>
public class VirusSplitVFX : MonoBehaviour
{
    [Header("Tap Punch")]
    [Tooltip("Duration of the scale punch animation on tap.")]
    [SerializeField] private float tapPunchDuration = 0.09f;
    [Tooltip("Scale overshoot on tap (e.g. 0.15 → peaks at 1.15).")]
    [SerializeField] private float tapPunchAmount   = 0.15f;

    [Header("Split VFX")]
    [Tooltip("Particle System played at the split point (centre → top/bottom).")]
    [SerializeField] private ParticleSystem splitParticles;

    [Header("Merge VFX")]
    [Tooltip("SpriteRenderer of the circle that shrinks toward the merge point.")]
    [SerializeField] private SpriteRenderer mergeCircle;
    [Tooltip("Starting scale of the merge circle.")]
    [SerializeField] private float mergeCircleStartScale = 1.2f;
    [Tooltip("Duration of the merge circle shrink animation.")]
    [SerializeField] private float mergeCircleDuration   = 0.12f;
    [Tooltip("Particle System played at the merge arrival point on the virus.")]
    [SerializeField] private ParticleSystem mergeArrivalParticles;

    private Coroutine _tapPunchA;
    private Coroutine _tapPunchB;

    private void Awake()
    {
        if (mergeCircle != null) mergeCircle.enabled = false;
    }

    /// <summary>Plays a scale punch on both virus transforms on tap.</summary>
    public void PlayTapPunch(Transform virusA, Transform virusB = null)
    {
        if (virusA != null)
        {
            if (_tapPunchA != null) StopCoroutine(_tapPunchA);
            _tapPunchA = StartCoroutine(ScalePunch(virusA, tapPunchAmount, tapPunchDuration));
        }
        if (virusB != null)
        {
            if (_tapPunchB != null) StopCoroutine(_tapPunchB);
            _tapPunchB = StartCoroutine(ScalePunch(virusB, tapPunchAmount, tapPunchDuration));
        }
    }

    /// <summary>Plays the split particle burst at a world position.</summary>
    public void PlaySplitVFX(Vector3 worldPosition)
    {
        if (splitParticles == null) return;
        splitParticles.transform.position = worldPosition;
        splitParticles.Play();
    }

    /// <summary>
    /// Plays the merge VFX: a circle shrinking from the top/bottom positions toward the centre,
    /// then a burst on the virus at the merge destination.
    /// </summary>
    public void PlayMergeVFX(Vector3 mergeDestination)
    {
        StartCoroutine(MergeCircleRoutine(mergeDestination));
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private IEnumerator ScalePunch(Transform target, float amount, float duration)
    {
        Vector3 original = target.localScale;
        float   elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / duration;
            float s  = 1f + amount * Mathf.Sin(t * Mathf.PI);
            target.localScale = original * s;
            yield return null;
        }

        target.localScale = original;
    }

    private IEnumerator MergeCircleRoutine(Vector3 destination)
    {
        if (mergeCircle == null) yield break;

        mergeCircle.transform.position = destination;
        mergeCircle.enabled            = true;
        mergeCircle.transform.localScale = Vector3.one * mergeCircleStartScale;

        float elapsed = 0f;
        while (elapsed < mergeCircleDuration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / mergeCircleDuration;
            float s  = Mathf.Lerp(mergeCircleStartScale, 0f, t);
            mergeCircle.transform.localScale = Vector3.one * s;
            yield return null;
        }

        mergeCircle.enabled = false;

        if (mergeArrivalParticles != null)
        {
            mergeArrivalParticles.transform.position = destination;
            mergeArrivalParticles.Play();
        }
    }
}
