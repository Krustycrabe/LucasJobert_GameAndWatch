using UnityEngine;

/// <summary>
/// Renders a LineRenderer trail that follows the virus's Y-movement history
/// and drifts left to simulate world scroll, with zero per-frame allocation
/// and no per-frame LineRenderer mesh dirty.
///
/// Ring buffer — O(1) push instead of an O(N) array shift each frame:
///   _buffer[_newest] always holds the current head position.
///   Logical index i maps to _buffer[(_newest - i + N) % N].
///   On sample push: only advance _newest by 1 (pointer move, no data copy).
///
/// Accumulated drift — O(1) drift update instead of touching all N slots:
///   Positions stored as world.x + _totalDrift ("drift space").
///   Each frame: _totalDrift += shift. Points appear to drift left automatically.
///   Read back: world.x = stored.x - _totalDrift.
///
/// positionCount is set ONCE in ConfigureLineRenderer (and restored in OnEnable).
/// Update() never sets positionCount → no per-frame GPU mesh upload.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class VirusSimulatedTrail : MonoBehaviour
{
    [Header("Trail")]
    [Tooltip("Number of samples (= visual length of the trail).")]
    [SerializeField] private int   pointCount     = 20;
    [Tooltip("Seconds between ring buffer pushes. Lower = denser Y history.")]
    [SerializeField] private float sampleInterval = 0.04f;
    [Tooltip("Initial left-spacing per slot when scroll speed is not yet known.")]
    [SerializeField] private float initSpacing    = 0.08f;

    [Header("Width")]
    [SerializeField] private float headWidth = 0.18f;
    [SerializeField] private float tailWidth = 0f;

    [Header("Color")]
    [SerializeField] private Color headColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private Color tailColor = new Color(1f, 1f, 1f, 0f);

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Player";
    [SerializeField] private int    sortingOrder     = -1;

    [Header("Material")]
    [SerializeField] private Material trailMaterial;

    private LineRenderer _lr;

    // Ring buffer: positions stored in drift space (world.x + _totalDrift).
    private Vector3[] _buffer;
    // Reusable output array for SetPositions — allocated once, never recreated.
    private Vector3[] _output;

    private int   _newest;      // _buffer index of the most recent sample
    private float _totalDrift;  // cumulative leftward drift since last PreFill
    private float _sampleTimer;
    private float _scrollSpeed;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _lr     = GetComponent<LineRenderer>();
        _buffer = new Vector3[pointCount];
        _output = new Vector3[pointCount];
        ConfigureLineRenderer();  // positionCount set ONCE here
    }

    private void OnEnable()
    {
        if (_buffer == null) return;
        _lr.positionCount = pointCount;  // restore after possible Clear()
        PreFill();
    }

    private void Update()
    {
        // O(1): accumulate drift — no loop over N points.
        float shift  = _scrollSpeed * Time.deltaTime;
        _totalDrift += shift;

        // O(1) ring push: move the head pointer only, no data copy.
        _sampleTimer += Time.deltaTime;
        if (_sampleTimer >= sampleInterval)
        {
            _sampleTimer -= sampleInterval;
            _newest       = (_newest + 1) % pointCount;
        }

        // Write current position into the head slot (drift-space encoding).
        Vector3 cur      = transform.position;
        _buffer[_newest] = new Vector3(cur.x + _totalDrift, cur.y, cur.z);

        // Build output for SetPositions: decode drift-space back to world-space.
        // O(N) read is unavoidable for a contiguous array required by SetPositions.
        for (int i = 0; i < pointCount; i++)
        {
            Vector3 s  = _buffer[(_newest - i + pointCount) % pointCount];
            _output[i] = new Vector3(s.x - _totalDrift, s.y, s.z);
        }

        // Single native call — positionCount NOT touched here.
        _lr.SetPositions(_output);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Called every frame by VirusController with the current scroll speed.</summary>
    public void SetScrollSpeed(float speed) => _scrollSpeed = speed;

    /// <summary>Hides the trail immediately (called on merge).</summary>
    public void Clear() => _lr.positionCount = 0;

    // ── Private ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds the ring buffer with a leftward spread so the trail is immediately
    /// visible from the first active frame without warm-up.
    /// </summary>
    private void PreFill()
    {
        _totalDrift  = 0f;
        _sampleTimer = 0f;
        _newest      = 0;

        Vector3 cur     = transform.position;
        float   spacing = _scrollSpeed > 0f
            ? sampleInterval * _scrollSpeed
            : initSpacing;

        // With _totalDrift=0, drift-space = world-space.
        // Logical index i → slot (N - i) % N → position cur.x - i*spacing.
        for (int i = 0; i < pointCount; i++)
        {
            int slot      = (pointCount - i) % pointCount;
            _buffer[slot] = new Vector3(cur.x - i * spacing, cur.y, cur.z);
        }

        for (int i = 0; i < pointCount; i++)
            _output[i] = _buffer[(pointCount - i) % pointCount]; // _totalDrift=0

        _lr.SetPositions(_output);
    }

    private void ConfigureLineRenderer()
    {
        _lr.useWorldSpace     = true;
        _lr.loop              = false;
        _lr.numCornerVertices = 2;   // was 4 — half the corner geometry cost
        _lr.numCapVertices    = 2;   // was 4 — half the cap geometry cost
        _lr.sortingLayerName  = sortingLayerName;
        _lr.sortingOrder      = sortingOrder;
        _lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lr.receiveShadows    = false;
        _lr.positionCount     = pointCount;  // set ONCE — never again at runtime

        if (trailMaterial != null)
            _lr.sharedMaterial = trailMaterial;

        _lr.widthCurve = new AnimationCurve(
            new Keyframe(0f, headWidth),
            new Keyframe(1f, tailWidth));

        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(headColor, 0f), new GradientColorKey(tailColor, 1f) },
            new[] { new GradientAlphaKey(headColor.a, 0f), new GradientAlphaKey(0f, 1f) });
        _lr.colorGradient = g;
    }

    private void OnValidate()
    {
        if (_lr != null) ConfigureLineRenderer();
    }
}
