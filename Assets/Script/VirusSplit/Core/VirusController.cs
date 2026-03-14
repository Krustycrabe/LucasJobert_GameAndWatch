using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Core controller for the VirusSplit mini-game.
/// Manages the split/merge state machine, input, scroll-speed ramp,
/// near-miss slow motion (condition: Split state + obstacle proximity),
/// and coordinates ObstacleSpawner, VirusScoreManager and ParallaxScroller layers.
///
/// Parallax: each ParallaxScroller keeps its own base speed in the Inspector.
/// VirusController scales all layers proportionally so that their relative depth
/// ratios are preserved as the game accelerates:
///   layer.ScrollSpeed = layer.BaseSpeed * (currentSpeed / initialScrollSpeed)
///
/// Animator triggers used: "Split", "Merge", "Die" on each virus Animator.
/// </summary>
public class VirusController : MonoBehaviour
{
    private static readonly int SplitHash = Animator.StringToHash("Split");
    private static readonly int MergeHash = Animator.StringToHash("Merge");
    private static readonly int DieHash   = Animator.StringToHash("Die");

    [Header("Config")]
    [SerializeField] private VirusSplitConfigSO config;

    [Header("Virus References")]
    [Tooltip("The top virus (also used when merged at centre).")]
    [SerializeField] private Transform virusA;
    [Tooltip("The bottom virus (shown only when split).")]
    [SerializeField] private Transform virusB;

    [Header("Scene Systems")]
    [SerializeField] private ObstacleSpawner   obstacleSpawner;
    [SerializeField] private VirusScoreManager scoreManager;
    [SerializeField] private SlowMotionManager slowMotionManager;
    [SerializeField] private VirusSplitVFX     vfx;

    [Header("Trail & Particles")]
    [Tooltip("Simulated LineRenderer trail on VirusA.")]
    [SerializeField] private VirusSimulatedTrail trailA;
    [Tooltip("Simulated LineRenderer trail on VirusB.")]
    [SerializeField] private VirusSimulatedTrail trailB;
    [Tooltip("Particle trail on VirusA/ParticleTrail child.")]
    [SerializeField] private VirusParticleTrail  particleA;
    [Tooltip("Particle trail on VirusB/ParticleTrail child.")]
    [SerializeField] private VirusParticleTrail  particleB;

    [Tooltip("All ParallaxScroller layers (far → near). Each keeps its own base speed in the Inspector.")]
    [SerializeField] private ParallaxScroller[] parallaxLayers;

    // ── State ─────────────────────────────────────────────────────────────────

    private enum VirusState { Merged, Splitting, Split, Merging }
    private VirusState _state = VirusState.Merged;

    private float     _currentScrollSpeed;
    private float     _elapsedTime;
    private bool      _gameOver;
    private Coroutine _transitionCoroutine;

    // Base speeds recorded at Start() from each layer's Inspector value.
    private float[] _layerBaseSpeeds;

    // ── Input ─────────────────────────────────────────────────────────────────

    private InputAction _tapAction;

    // ── Cached refs ───────────────────────────────────────────────────────────

    private Animator _animA;
    private Animator _animB;

    /// <summary>True when the virus is in any split-related state.</summary>
    public bool IsSplit => _state == VirusState.Split
                        || _state == VirusState.Splitting
                        || _state == VirusState.Merging;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        // Press on touch/click fires immediately on contact — no release required.
        // This gives the lowest possible latency for a tap mechanic.
        _tapAction = new InputAction("Tap", InputActionType.Button);
        _tapAction.AddBinding("<Touchscreen>/primaryTouch/press");
        _tapAction.AddBinding("<Mouse>/leftButton");
        _tapAction.performed += OnTap;
        _tapAction.Enable();

        _animA = virusA != null ? virusA.GetComponent<Animator>() : null;
        _animB = virusB != null ? virusB.GetComponent<Animator>() : null;
    }

    private void Start()
    {
        _currentScrollSpeed = config.initialScrollSpeed;

        // Record each layer's Inspector speed as its depth anchor.
        _layerBaseSpeeds = new float[parallaxLayers.Length];
        for (int i = 0; i < parallaxLayers.Length; i++)
            _layerBaseSpeeds[i] = parallaxLayers[i] != null ? parallaxLayers[i].ScrollSpeed : 1f;

        SetVirusBVisible(false);
        SetLocalY(virusA, 0f);

        slowMotionManager?.Initialize(config, GetVirusIsSplit, GetActiveVirusPositions);
        scoreManager?.Initialize(config);
        obstacleSpawner?.Initialize(config, GetActiveVirusPositions);
    }

    private void OnDestroy()
    {
        _tapAction.performed -= OnTap;
        _tapAction.Disable();
        _tapAction.Dispose();
    }

    private void OnEnable()  => GameOverEvents.OnGameOver += HandleGameOver;
    private void OnDisable() => GameOverEvents.OnGameOver -= HandleGameOver;

    private void Update()
    {
        if (_gameOver) return;

        _elapsedTime        += Time.deltaTime;
        _currentScrollSpeed  = Mathf.Min(
            config.initialScrollSpeed + _elapsedTime * config.speedIncreaseRate,
            config.maxScrollSpeed);

        scoreManager?.UpdateSpeed(_currentScrollSpeed);
        obstacleSpawner?.UpdateSpeed(_currentScrollSpeed);
        trailA?.SetScrollSpeed(_currentScrollSpeed);
        trailB?.SetScrollSpeed(_currentScrollSpeed);
        particleA?.SetScrollSpeed(_currentScrollSpeed);
        particleB?.SetScrollSpeed(_currentScrollSpeed);

        // Scale all parallax layers proportionally — preserves depth ratios.
        float ratio = _currentScrollSpeed / Mathf.Max(config.initialScrollSpeed, 0.001f);
        for (int i = 0; i < parallaxLayers.Length; i++)
            if (parallaxLayers[i] != null)
                parallaxLayers[i].ScrollSpeed = _layerBaseSpeeds[i] * ratio;
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void OnTap(InputAction.CallbackContext ctx)
    {
        if (_gameOver) return;
        switch (_state)
        {
            case VirusState.Merged: BeginSplit(); break;
            case VirusState.Split:  BeginMerge(); break;
        }
    }

    // ── State Machine ─────────────────────────────────────────────────────────

    private void BeginSplit()
    {
        _state = VirusState.Splitting;

        // Fire slow-mo if the player is inside an obstacle proximity zone.
        slowMotionManager?.TryTriggerSlowMo();

        vfx?.PlayTapPunch(virusA);
        vfx?.PlaySplitVFX(virusA.position);

        _animA?.SetTrigger(SplitHash);
        _animB?.SetTrigger(SplitHash);

        // Set scroll speed on trailB BEFORE activating VirusB so that
        // OnEnable → PreFill() uses the correct speed and the trail is
        // immediately visible with proper X spacing.
        trailB?.SetScrollSpeed(_currentScrollSpeed);
        particleB?.SetScrollSpeed(_currentScrollSpeed);

        SetLocalY(virusB, 0f);
        SetVirusBVisible(true);

        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(TransitionRoutine(
            virusA, GetLocalY(virusA), config.splitTopY,
            virusB, GetLocalY(virusB), config.splitBottomY,
            config.splitDuration,
            () => _state = VirusState.Split));
    }

    private void BeginMerge()
    {
        _state = VirusState.Merging;

        // Fire slow-mo if the player is inside an obstacle proximity zone.
        slowMotionManager?.TryTriggerSlowMo();

        vfx?.PlayTapPunch(virusA, virusB);
        vfx?.PlayMergeVFX(new Vector3(virusA.position.x, 0f, virusA.position.z));

        _animA?.SetTrigger(MergeHash);
        _animB?.SetTrigger(MergeHash);

        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(TransitionRoutine(
            virusA, GetLocalY(virusA), 0f,
            virusB, GetLocalY(virusB), 0f,
            config.mergeDuration,
            () => { _state = VirusState.Merged; trailB?.Clear(); particleB?.Pause(); SetVirusBVisible(false); }));
    }

    private IEnumerator TransitionRoutine(
        Transform tA, float fromA, float toA,
        Transform tB, float fromB, float toB,
        float duration, Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Use unscaledDeltaTime so slow-motion does not extend the transition
            // and block input for longer than intended.
            elapsed += Time.unscaledDeltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            float c  = config.moveCurve.Evaluate(t);
            SetLocalY(tA, Mathf.Lerp(fromA, toA, c));
            SetLocalY(tB, Mathf.Lerp(fromB, toB, c));
            yield return null;
        }
        SetLocalY(tA, toA);
        SetLocalY(tB, toB);
        onComplete?.Invoke();
        _transitionCoroutine = null;
    }

    // ── Game Over ─────────────────────────────────────────────────────────────

    private void HandleGameOver()
    {
        if (_gameOver) return;
        _gameOver = true;
        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _animA?.SetTrigger(DieHash);
        _animB?.SetTrigger(DieHash);
        foreach (var layer in parallaxLayers)
            if (layer != null) layer.enabled = false;
    }

    // ── Delegates ─────────────────────────────────────────────────────────────

    /// <summary>Returns the world positions of all currently active virus colliders.</summary>
    public Vector2[] GetActiveVirusPositions()
    {
        if (IsSplit && virusB != null && virusB.gameObject.activeSelf)
            return new[] { (Vector2)virusA.position, (Vector2)virusB.position };
        return new[] { (Vector2)virusA.position };
    }

    private bool GetVirusIsSplit() => IsSplit;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetVirusBVisible(bool visible)
    {
        if (virusB != null) virusB.gameObject.SetActive(visible);
    }

    private static void  SetLocalY(Transform t, float y)
    {
        if (t == null) return;
        Vector3 p = t.localPosition;
        p.y = y;
        t.localPosition = p;
    }

    private static float GetLocalY(Transform t) => t != null ? t.localPosition.y : 0f;
}
