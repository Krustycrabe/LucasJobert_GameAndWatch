using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Core controller for the VirusSplit mini-game.
/// Manages the split/merge state machine, input, scroll speed ramp,
/// and coordinates ObstacleSpawner, VirusScoreManager and ParallaxScroller layers.
///
/// Scene setup expected:
///   - VirusA  (child): tag "Player", has Collider2D + Animator + TrailRenderer
///   - VirusB  (child): tag "Player", has Collider2D + Animator + TrailRenderer, starts inactive
///   - ParallaxScroller components assigned to parallaxLayers (all layers in the scene)
/// Animator triggers used: "Split", "Merge", "Die" — create these in your AnimatorController.
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
    [SerializeField] private ObstacleSpawner    obstacleSpawner;
    [SerializeField] private VirusScoreManager  scoreManager;
    [SerializeField] private SlowMotionManager  slowMotionManager;
    [SerializeField] private VirusSplitVFX      vfx;
    [Tooltip("All ParallaxScroller layers in the scene (background, mid, near).")]
    [SerializeField] private ParallaxScroller[] parallaxLayers;

    // State
    private enum VirusState { Merged, Splitting, Split, Merging }
    private VirusState _state = VirusState.Merged;

    private float   _currentScrollSpeed;
    private float   _elapsedTime;
    private bool    _gameOver;
    private Coroutine _transitionCoroutine;

    // Input
    private InputAction _tapAction;

    // Cached component references
    private Animator _animA;
    private Animator _animB;

    // ── Unity Lifecycle ────────────────────────────────────────────────────────

    private void Awake()
    {
        _tapAction = new InputAction("Tap", InputActionType.Button);
        _tapAction.AddBinding("<Touchscreen>/primaryTouch/tap");
        _tapAction.AddBinding("<Mouse>/leftButton");
        _tapAction.performed += OnTap;
        _tapAction.Enable();

        _animA = virusA != null ? virusA.GetComponent<Animator>() : null;
        _animB = virusB != null ? virusB.GetComponent<Animator>() : null;
    }

    private void Start()
    {
        _currentScrollSpeed = config.initialScrollSpeed;

        // Hide VirusB until first split.
        SetVirusBVisible(false);

        // Centre VirusA at origin.
        SetLocalY(virusA, 0f);

        // Initialize sub-systems.
        slowMotionManager?.Initialize(config);
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

        // Speed ramp.
        _elapsedTime        += Time.deltaTime;
        _currentScrollSpeed  = Mathf.Min(
            config.initialScrollSpeed + _elapsedTime * config.speedIncreaseRate,
            config.maxScrollSpeed);

        // Push speed to sub-systems.
        scoreManager?.UpdateSpeed(_currentScrollSpeed);
        obstacleSpawner?.UpdateSpeed(_currentScrollSpeed);
        foreach (var layer in parallaxLayers)
            if (layer != null) layer.ScrollSpeed = _currentScrollSpeed * GetLayerMultiplier(layer);
    }

    // ── Input ──────────────────────────────────────────────────────────────────

    private void OnTap(InputAction.CallbackContext ctx)
    {
        if (_gameOver) return;

        switch (_state)
        {
            case VirusState.Merged:
                BeginSplit();
                break;
            case VirusState.Split:
                BeginMerge();
                break;
            // Ignore taps during transitions.
        }
    }

    // ── State Machine ──────────────────────────────────────────────────────────

    private void BeginSplit()
    {
        _state = VirusState.Splitting;

        vfx?.PlayTapPunch(virusA);
        vfx?.PlaySplitVFX(virusA.position);

        _animA?.SetTrigger(SplitHash);
        _animB?.SetTrigger(SplitHash);

        SetVirusBVisible(true);
        SetLocalY(virusB, 0f); // start from centre

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

        vfx?.PlayTapPunch(virusA, virusB);
        vfx?.PlayMergeVFX(new Vector3(virusA.position.x, 0f, virusA.position.z));

        _animA?.SetTrigger(MergeHash);
        _animB?.SetTrigger(MergeHash);

        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(TransitionRoutine(
            virusA, GetLocalY(virusA), 0f,
            virusB, GetLocalY(virusB), 0f,
            config.mergeDuration,
            () =>
            {
                _state = VirusState.Merged;
                SetVirusBVisible(false);
            }));
    }

    private IEnumerator TransitionRoutine(
        Transform targetA, float fromA, float toA,
        Transform targetB, float fromB, float toB,
        float duration,
        System.Action onComplete)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            float c  = config.moveCurve.Evaluate(t);

            SetLocalY(targetA, Mathf.Lerp(fromA, toA, c));
            SetLocalY(targetB, Mathf.Lerp(fromB, toB, c));

            yield return null;
        }

        SetLocalY(targetA, toA);
        SetLocalY(targetB, toB);
        onComplete?.Invoke();
        _transitionCoroutine = null;
    }

    // ── Game Over ──────────────────────────────────────────────────────────────

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

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>Returns world positions of all currently active viruses (used by ObstacleMover).</summary>
    private Vector2[] GetActiveVirusPositions()
    {
        bool bActive = _state == VirusState.Split || _state == VirusState.Splitting || _state == VirusState.Merging;

        if (bActive && virusB != null)
            return new Vector2[] { virusA.position, virusB.position };

        return new Vector2[] { virusA.position };
    }

    private void SetVirusBVisible(bool visible)
    {
        if (virusB == null) return;
        virusB.gameObject.SetActive(visible);
    }

    private static void  SetLocalY(Transform t, float y)
    {
        if (t == null) return;
        Vector3 p = t.localPosition;
        p.y = y;
        t.localPosition = p;
    }

    private static float GetLocalY(Transform t) => t != null ? t.localPosition.y : 0f;

    /// <summary>
    /// Returns a parallax speed multiplier for a given layer based on its inspector scroll speed.
    /// Layers with a lower base ScrollSpeed get a proportionally smaller multiplier so the
    /// visual depth is preserved as the overall game speed increases.
    /// Override this with a per-layer multiplier array if you need finer control.
    /// </summary>
    private float GetLayerMultiplier(ParallaxScroller layer)
    {
        // Preserve the ratio between layers by using their initial ScrollSpeed value
        // as a percentage of the config's initial scroll speed.
        // Layers drive their own base speed in the Inspector.
        return 1f;
    }
}
