using System.Collections;
using UnityEngine;

/// <summary>
/// Manages all VFX for the VirusSplit mini-game:
///   - Scale punch on tap
///   - Split particle burst (configured procedurally in Awake — no manual setup needed)
///   - Merge circle shrink + arrival burst
///
/// Triggers its own Animator ("Split", "Merge") when VFX play.
/// All particles are configured at runtime; only the component references need
/// to be assigned in the Inspector (splitParticles, mergeCircle, mergeArrivalParticles).
/// </summary>
public class VirusSplitVFX : MonoBehaviour
{
    private static readonly int SplitHash = Animator.StringToHash("Split");
    private static readonly int MergeHash = Animator.StringToHash("Merge");

    [Header("Tap Punch")]
    [SerializeField] private float tapPunchDuration = 0.09f;
    [SerializeField] private float tapPunchAmount   = 0.15f;

    [Header("Split VFX")]
    [Tooltip("ParticleSystem on VFX/SplitParticles child.")]
    [SerializeField] private ParticleSystem splitParticles;
    [Tooltip("Material for particle bursts. Must use URP Particles/Unlit shader. " +
             "Leave empty to auto-create a plain white material at runtime.")]
    [SerializeField] private Material particleMaterial;

    [Header("Merge VFX")]
    [Tooltip("SpriteRenderer on VFX/MergeCircle child (assign any white circle sprite).")]
    [SerializeField] private SpriteRenderer mergeCircle;
    [SerializeField] private float mergeCircleStartScale = 1.4f;
    [SerializeField] private float mergeCircleDuration   = 0.14f;
    [Tooltip("Optional extra burst on arrival (can reuse splitParticles or a separate PS).")]
    [SerializeField] private ParticleSystem mergeArrivalParticles;

    private Animator  _animator;
    private Coroutine _tapPunchA;
    private Coroutine _tapPunchB;

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (mergeCircle != null) mergeCircle.enabled = false;

        ConfigureSplitParticles(splitParticles, particleMaterial);
        ConfigureSplitParticles(mergeArrivalParticles, particleMaterial);
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>Scale punch on both virus transforms on tap.</summary>
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

    /// <summary>Burst split particles at the given world position and trigger the Animator.</summary>
    public void PlaySplitVFX(Vector3 worldPosition)
    {
        _animator?.SetTrigger(SplitHash);

        if (splitParticles != null)
        {
            splitParticles.transform.position = worldPosition;
            splitParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            splitParticles.Play();
        }
    }

    /// <summary>Shrink the merge circle toward the destination and trigger the Animator.</summary>
    public void PlayMergeVFX(Vector3 mergeDestination)
    {
        _animator?.SetTrigger(MergeHash);
        StartCoroutine(MergeCircleRoutine(mergeDestination));
    }

    // ── Private ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Configures a ParticleSystem as a one-shot outward burst.
    /// If mat is null, creates a plain white URP Particles/Unlit material at runtime
    /// so particles never appear pink.
    /// </summary>
    private static void ConfigureSplitParticles(ParticleSystem ps, Material mat)
    {
        if (ps == null) return;

        var main             = ps.main;
        main.loop            = false;
        main.playOnAwake     = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor      = new ParticleSystem.MinMaxGradient(Color.white);

        var emission         = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

        var shape            = ps.shape;
        shape.enabled        = true;
        shape.shapeType      = ParticleSystemShapeType.Circle;
        shape.radius         = 0.15f;
        shape.radiusThickness = 0f;

        var sol              = ps.sizeOverLifetime;
        sol.enabled          = true;
        sol.size             = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)));

        var col              = ps.colorOverLifetime;
        col.enabled          = true;
        var g                = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var rend             = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode      = ParticleSystemRenderMode.Billboard;
        rend.sortingLayerName = "Player";
        rend.sortingOrder    = 1;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Assign material — fall back to a runtime-created URP white material if none assigned.
        if (mat != null)
        {
            rend.sharedMaterial = mat;
        }
        else
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
                rend.sharedMaterial = new Material(shader) { name = "_VFX_Particle_Auto" };
        }
    }

    private IEnumerator ScalePunch(Transform target, float amount, float duration)
    {
        Vector3 original = target.localScale;
        float   elapsed  = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            target.localScale = original * (1f + amount * Mathf.Sin(elapsed / duration * Mathf.PI));
            yield return null;
        }
        target.localScale = original;
    }

    private IEnumerator MergeCircleRoutine(Vector3 destination)
    {
        if (mergeCircle == null) yield break;

        mergeCircle.transform.position   = destination;
        mergeCircle.transform.localScale  = Vector3.one * mergeCircleStartScale;
        mergeCircle.enabled               = true;

        float elapsed = 0f;
        while (elapsed < mergeCircleDuration)
        {
            elapsed += Time.deltaTime;
            mergeCircle.transform.localScale = Vector3.one *
                Mathf.Lerp(mergeCircleStartScale, 0f, elapsed / mergeCircleDuration);
            yield return null;
        }

        mergeCircle.enabled = false;

        if (mergeArrivalParticles != null)
        {
            mergeArrivalParticles.transform.position = destination;
            mergeArrivalParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            mergeArrivalParticles.Play();
        }
    }
}

