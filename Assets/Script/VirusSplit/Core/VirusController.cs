using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Core controller for the VirusSplit mini-game.
///
/// INPUT MODEL — why a flag instead of Disable()/Enable():
///   Unity InputSystem dispatches ALL queued events at the start of each frame,
///   before any C# code runs. Calling _tapAction.Disable() from inside a
///   performed callback does NOT prevent other already-queued events from firing
///   in the same dispatch cycle. This caused a race where BeginSplit fired, then
///   BeginMerge fired in the same frame, StopCoroutine killed the split coroutine
///   before its onComplete ran, Enable() was never called, and the input was
///   permanently frozen — with no GameOver panel and the player still moving.
///
///   Fix: _inputLocked is a plain bool checked at the top of OnTap. It is set
///   synchronously before any transition work, so it is always visible to
///   subsequent callbacks in the same dispatch cycle. The InputAction itself is
///   enabled for the lifetime of the game — we never call Disable()/Enable() from
///   inside a callback.
///
///   INPUT ASSET: Assets/Inputs/VirusSplitActions.inputactions
///   Action Map : VirusSplit → Tap (Button)
/// </summary>
public class VirusController : MonoBehaviour
{
    private static readonly int SplitHash = Animator.StringToHash("Split");
    private static readonly int MergeHash = Animator.StringToHash("Merge");
    private static readonly int DieHash   = Animator.StringToHash("Die");

    [Header("Config")]
    [SerializeField] private VirusSplitConfigSO config;

    [Header("Virus References")]
    [SerializeField] private Transform virusA;
    [SerializeField] private Transform virusB;

    [Header("Scene Systems")]
    [SerializeField] private ObstacleSpawner   obstacleSpawner;
    [SerializeField] private VirusScoreManager scoreManager;
    [SerializeField] private SlowMotionManager slowMotionManager;
    [SerializeField] private VirusSplitVFX     vfx;

    [Header("Trail & Particles")]
    [SerializeField] private VirusSimulatedTrail trailA;
    [SerializeField] private VirusSimulatedTrail trailB;
    [SerializeField] private VirusParticleTrail  particleA;
    [SerializeField] private VirusParticleTrail  particleB;

    [SerializeField] private ParallaxScroller[] parallaxLayers;

    [Header("Input")]
    [Tooltip("Minimum real-time gap (seconds) between two accepted inputs. Prevents double-fire.")]
    [SerializeField] private float inputGuardTime = 0.06f;

    [Tooltip("Seconds after scene load before the first input is accepted. " +
             "Covers the obstacle pool prewarm spike so taps during init cannot corrupt state.")]
    [SerializeField] private float inputStartDelay = 0.5f;

    // ── State ─────────────────────────────────────────────────────────────────

    private enum VirusState { Merged, Splitting, Split, Merging }
    private VirusState _state = VirusState.Merged;

    private float     _currentScrollSpeed;
    private float     _elapsedTime;
    private bool      _gameOver;
    private bool      _inputLocked;       // true during transitions — checked in OnTap
    private bool      _inputReady;        // false until prewarm delay has elapsed
    private Coroutine _transitionCoroutine;
    private float     _lastInputRealTime = -999f;

    private float[]  _layerBaseSpeeds;

    private readonly Vector2[] _splitPositions  = new Vector2[2];
    private readonly Vector2[] _singlePositions = new Vector2[1];

    private InputAction _tapAction;
    private Animator    _animA;
    private Animator    _animB;

    // ── Input asset reference ─────────────────────────────────────────────────
    // Assigned via Inspector once VirusSplitActions.inputactions is generated.
    [Header("Input")]
    [Tooltip("Drag the generated VirusSplitActions asset here.")]
    [SerializeField] private InputActionAsset inputActionsAsset;

    public bool IsSplit => _state is VirusState.Split or VirusState.Splitting or VirusState.Merging;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Use the asset if assigned and valid, otherwise fall back to a runtime-created action.
        if (inputActionsAsset != null)
        {
            var map = inputActionsAsset.FindActionMap("VirusSplit");
            if (map != null)
                _tapAction = map.FindAction("Tap");
        }

        if (_tapAction == null)
        {
            Debug.LogWarning("[VirusController] Input asset not found or misconfigured — using runtime fallback bindings.");
            _tapAction = new InputAction("Tap", InputActionType.Button);
            _tapAction.AddBinding("<Touchscreen>/primaryTouch/press");
            _tapAction.AddBinding("<Mouse>/leftButton");
        }

        _tapAction.performed += OnTap;
        _tapAction.Enable();

        _animA = virusA != null ? virusA.GetComponent<Animator>() : null;
        _animB = virusB != null ? virusB.GetComponent<Animator>() : null;

        if (particleB != null) particleB.ControlledStart = true;
    }

    private void Start()
    {
        _currentScrollSpeed = config.initialScrollSpeed;

        _layerBaseSpeeds = new float[parallaxLayers.Length];
        for (int i = 0; i < parallaxLayers.Length; i++)
            _layerBaseSpeeds[i] = parallaxLayers[i] != null ? parallaxLayers[i].ScrollSpeed : 1f;

        SetVirusBVisible(false);
        SetLocalY(virusA, 0f);

        // Cache base scales before the first punch so VFX always restores to the
        // correct reference value even on StopCoroutine (try/finally is unreliable in Unity).
        vfx?.CacheBaseScales(virusA, virusB);

        slowMotionManager?.Initialize(config, GetVirusIsSplit, GetActiveVirusPositions);
        scoreManager?.Initialize(config);
        obstacleSpawner?.Initialize(config, GetActiveVirusPositions);

        // Lock input during prewarm. _inputReady is a plain bool checked in OnTap —
        // safe to set from anywhere, unlike _tapAction.Disable() which is unsafe from callbacks.
        _inputLocked = true;
        _inputReady  = false;
        StartCoroutine(EnableInputAfterDelay());
    }

    private IEnumerator EnableInputAfterDelay()
    {
        Debug.Log($"[VirusController] Input locked — waiting {inputStartDelay}s for pool prewarm");
        yield return new WaitForSecondsRealtime(inputStartDelay);
        if (!_gameOver)
        {
            _inputLocked = false;
            _inputReady  = true;
            Debug.Log("[VirusController] Input unlocked — ready");
        }
    }

    private void OnDestroy()
    {
        if (_tapAction == null) return;
        _tapAction.performed -= OnTap;
        if (inputActionsAsset == null)
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

        float ratio = _currentScrollSpeed / Mathf.Max(config.initialScrollSpeed, 0.001f);
        for (int i = 0; i < parallaxLayers.Length; i++)
            if (parallaxLayers[i] != null)
                parallaxLayers[i].ScrollSpeed = _layerBaseSpeeds[i] * ratio;
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void OnTap(InputAction.CallbackContext ctx)
    {
        // _inputLocked is a plain bool — always safe to read from within a callback,
        // unlike _tapAction.enabled which reflects a deferred state in InputSystem 1.18.
        if (_gameOver || _inputLocked || !_inputReady) return;

        // Secondary guard against InputSystem double-fire (same real-time frame).
        float now = Time.realtimeSinceStartup;
        if (now - _lastInputRealTime < inputGuardTime) return;
        _lastInputRealTime = now;

        // Lock IMMEDIATELY — before any transition work — so any other queued callback
        // in the same InputSystem dispatch cycle sees _inputLocked = true and exits.
        _inputLocked = true;

        Debug.Log($"[VirusController] OnTap — state={_state} timeScale={Time.timeScale:F2}");

        switch (_state)
        {
            case VirusState.Merged: BeginSplit(); break;
            case VirusState.Split:  BeginMerge(); break;
            default:
                // Should never happen — unlock so the player is not permanently stuck.
                Debug.LogWarning($"[VirusController] Unexpected state={_state} on tap — unlocking");
                _inputLocked = false;
                break;
        }
    }

    // ── State Machine ─────────────────────────────────────────────────────────

    private void BeginSplit()
    {
        // _inputLocked already set to true in OnTap before this call.
        _state = VirusState.Splitting;

        slowMotionManager?.TryTriggerSlowMo();
        vfx?.PlayTapPunch(virusA);
        vfx?.PlaySplitVFX(virusA.position);

        TriggerAnimator(_animA, SplitHash, MergeHash);

        trailB?.SetScrollSpeed(_currentScrollSpeed);
        particleB?.SetScrollSpeed(_currentScrollSpeed);

        SetLocalY(virusB, 0f);
        SetVirusBVisible(true);
        particleB?.Resume();
        TriggerAnimator(_animB, SplitHash, MergeHash);

        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(TransitionRoutine(
            virusA, GetLocalY(virusA), config.splitTopY,
            virusB, GetLocalY(virusB), config.splitBottomY,
            config.splitDuration,
            () =>
            {
                _state       = VirusState.Split;
                _inputLocked = false; // unlock via plain bool — no InputSystem API call
                Debug.Log("[VirusController] Split complete — input unlocked");
            }));
    }

    private void BeginMerge()
    {
        // _inputLocked already set to true in OnTap before this call.
        _state = VirusState.Merging;

        slowMotionManager?.TryTriggerSlowMo();
        vfx?.PlayTapPunch(virusA, virusB);
        vfx?.PlayMergeVFX(new Vector3(virusA.position.x, 0f, virusA.position.z));

        TriggerAnimator(_animA, MergeHash, SplitHash);
        TriggerAnimator(_animB, MergeHash, SplitHash);

        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(TransitionRoutine(
            virusA, GetLocalY(virusA), 0f,
            virusB, GetLocalY(virusB), 0f,
            config.mergeDuration,
            () =>
            {
                _state = VirusState.Merged;
                trailB?.Clear();
                particleB?.Pause();
                SetVirusBVisible(false);
                _inputLocked = false; // unlock via plain bool — no InputSystem API call
                Debug.Log("[VirusController] Merge complete — input unlocked");
            }));
    }

    private IEnumerator TransitionRoutine(
        Transform tA, float fromA, float toA,
        Transform tB, float fromB, float toB,
        float duration, Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
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
        Debug.Log("[VirusController] HandleGameOver triggered");
        _gameOver    = true;
        _inputLocked = true;

        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }

        _animA?.ResetTrigger(SplitHash);
        _animA?.ResetTrigger(MergeHash);
        _animB?.ResetTrigger(SplitHash);
        _animB?.ResetTrigger(MergeHash);

        _animA?.SetTrigger(DieHash);
        _animB?.SetTrigger(DieHash);

        foreach (var layer in parallaxLayers)
            if (layer != null) layer.enabled = false;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public Vector2[] GetActiveVirusPositions()
    {
        if (IsSplit && virusB != null && virusB.gameObject.activeSelf)
        {
            _splitPositions[0] = virusA.position;
            _splitPositions[1] = virusB.position;
            return _splitPositions;
        }
        _singlePositions[0] = virusA.position;
        return _singlePositions;
    }

    private bool GetVirusIsSplit() => IsSplit;

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Resets the opposite trigger before setting the target one.
    /// Prevents trigger accumulation in the Animator queue when inputs are spammed,
    /// and guards against Animators with no controller assigned.
    /// </summary>
    private static void TriggerAnimator(Animator anim, int triggerHash, int cancelHash)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return;
        anim.ResetTrigger(cancelHash);
        anim.SetTrigger(triggerHash);
    }

    private void SetVirusBVisible(bool visible)
    {
        if (virusB != null) virusB.gameObject.SetActive(visible);
    }

    private static void SetLocalY(Transform t, float y)
    {
        if (t == null) return;
        Vector3 p = t.localPosition;
        p.y = y;
        t.localPosition = p;
    }

    private static float GetLocalY(Transform t) => t != null ? t.localPosition.y : 0f;
}
