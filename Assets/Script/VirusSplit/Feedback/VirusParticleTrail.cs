using UnityEngine;

/// <summary>
/// Lightweight particle trail for a virus in world-scroll space.
///
/// Design constraints to avoid any per-frame ParticleSystem mutation:
///   - All ParticleSystem modules configured ONCE in Configure() (called from Awake).
///   - VelocityOverLifetime X/Y/Z all use TwoConstants mode (same mode = zero warning spam).
///   - No SetScrollSpeed() mutation — particles drift in Local space without needing
///     the actual scroll speed value.
///   - sizeOverLifetime uses a two-constant range [1, 0] mapped through a curve
///     only evaluated at particle birth (not per-frame per-particle).
///   - maxParticles = 25 (emissionRate 12 × lifetime 0.25s = ~3 alive at once; 25 is safe headroom).
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class VirusParticleTrail : MonoBehaviour
{
    [Header("Emission")]
    [SerializeField] private float emissionRate = 12f;
    [SerializeField] private float lifetime     = 0.25f;

    [Header("Size")]
    [SerializeField] private float startSizeMin = 0.04f;
    [SerializeField] private float startSizeMax = 0.07f;

    [Header("Color")]
    [SerializeField] private Color startColor = new Color(1f, 1f, 1f, 0.55f);

    [Header("Spread")]
    [SerializeField] private float spreadY = 0.12f;

    [Header("Sorting")]
    [SerializeField] private string sortingLayer = "Player";
    [SerializeField] private int    sortingOrder = -2;

    [Header("Material")]
    [SerializeField] private Material particleMaterial;

    private ParticleSystem _ps;
    private bool           _configured;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        Configure();
    }

    // Set to true by the controller before the first SetActive(true) on VirusB.
    // When true, Start() skips Play() because Resume() already started the system.
    public bool ControlledStart { get; set; }

    private void Start()
    {
        if (!_configured) Configure();

        // VirusA  : ControlledStart is never set → Play() fires at scene start. ✓
        // VirusB  : ControlledStart is set by VirusController BEFORE the first
        //           SetActive(true). Start() is deferred to the next frame by Unity,
        //           so Resume() already ran → do NOT Play() again or it restarts
        //           the ParticleSystem and drops the first emission burst.
        if (!ControlledStart)
            _ps.Play();
    }

    private void OnEnable()
    {
        // Do NOT auto-play here — VirusController calls Resume() explicitly
        // after SetVirusBVisible(true) so the emission rate is always correct.
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Stub kept for API compatibility — no per-frame mutation required.</summary>
    public void SetScrollSpeed(float speed) { }

    /// <summary>Stops emission and clears live particles (called when VirusB merges back).</summary>
    public void Pause()
    {
        if (_ps == null) return;
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var e = _ps.emission;
        e.rateOverTime = 0f;
    }

    /// <summary>Restores emission rate and plays (called when VirusB activates on split).</summary>
    public void Resume()
    {
        if (_ps == null) return;
        var e = _ps.emission;
        e.rateOverTime = emissionRate;
        _ps.Play();
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void Configure()
    {
        if (_ps == null) return;

        // ── Main ──────────────────────────────────────────────────────────────
        var main             = _ps.main;
        main.loop            = true;
        main.playOnAwake     = false;
        main.startLifetime   = lifetime;
        // Random size range using TwoConstants — no AnimationCurve evaluation at runtime.
        main.startSize       = new ParticleSystem.MinMaxCurve(startSizeMin, startSizeMax);
        main.startSpeed      = 0f;
        // Local space: virus is stationary in X; particles drift in local frame naturally.
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.gravityModifier = 0f;
        // headroom = ceil(emissionRate × lifetime) × 2. At 12 × 0.25 = 3, cap at 25.
        main.maxParticles    = 25;
        main.startColor      = startColor;

        // ── Emission ──────────────────────────────────────────────────────────
        var emission          = _ps.emission;
        emission.rateOverTime = emissionRate;

        // ── Shape ─────────────────────────────────────────────────────────────
        var shape             = _ps.shape;
        shape.enabled         = true;
        shape.shapeType       = ParticleSystemShapeType.Circle;
        shape.radius          = 0.01f;
        shape.radiusThickness = 1f;

        // ── Velocity Over Lifetime ─────────────────────────────────────────────
        // All three axes MUST use the same MinMaxCurve mode (TwoConstants here).
        // Never reassigned at runtime → zero "curves must all be in the same mode" warning.
        var vol  = _ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space   = ParticleSystemSimulationSpace.Local;
        vol.x       = new ParticleSystem.MinMaxCurve(-0.25f, -0.08f); // slight leftward drift
        vol.y       = new ParticleSystem.MinMaxCurve(-spreadY, spreadY);
        vol.z       = new ParticleSystem.MinMaxCurve(0f, 0f);          // same mode: TwoConstants

        // ── Color Over Lifetime ───────────────────────────────────────────────
        var col = _ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(startColor.r, startColor.g, startColor.b), 0f),
                    new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(startColor.a, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        // ── Size Over Lifetime ────────────────────────────────────────────────
        // Simple linear shrink via AnimationCurve — allocated ONCE here, not per frame.
        var sol   = _ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size    = new ParticleSystem.MinMaxCurve(
            1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

        // ── Renderer ──────────────────────────────────────────────────────────
        var rend              = _ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode       = ParticleSystemRenderMode.Billboard;
        rend.sortingLayerName = sortingLayer;
        rend.sortingOrder     = sortingOrder;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        if (particleMaterial != null)
            rend.sharedMaterial = particleMaterial;
        // No Shader.Find fallback: assign VFX_Particle.mat in the Inspector.

        _configured = true;
    }
}
