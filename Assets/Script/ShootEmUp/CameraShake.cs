using UnityEngine;

/// <summary>
/// Singleton camera shake controller using Perlin noise.
/// Supports one-shot shakes (e.g. enemy death) and continuous shakes (e.g. laser).
/// Attach to the Main Camera. All shakes are expressed in world units.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("One-Shot Presets")]
    [SerializeField] private ShakeData enemyDeathShake = new ShakeData(0.06f, 0.20f);
    [SerializeField] private ShakeData bossDeathShake  = new ShakeData(0.22f, 0.50f);

    [Header("Continuous Presets")]
    [SerializeField] private ShakeData laserContinuousShake = new ShakeData(0.03f, 0f);

    // Accessors used by other systems to retrieve preset values.
    public ShakeData EnemyDeathShake      => enemyDeathShake;
    public ShakeData BossDeathShake       => bossDeathShake;
    public ShakeData LaserContinuousShake => laserContinuousShake;

    private Vector3 _originLocalPosition;

    // One-shot state
    private float _oneShotMagnitude;
    private float _oneShotTimer;
    private float _oneShotDuration;

    // Continuous state
    private float _continuousMagnitude;

    // Perlin seed so multiple instances don't sync
    private float _perlinSeedX;
    private float _perlinSeedY;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _originLocalPosition = transform.localPosition;
        _perlinSeedX = Random.Range(0f, 100f);
        _perlinSeedY = Random.Range(0f, 100f);
    }

    private void LateUpdate()
    {
        Vector3 offset = Vector3.zero;

        // One-shot shake — magnitude fades linearly to zero over duration.
        if (_oneShotTimer > 0f)
        {
            _oneShotTimer -= Time.unscaledDeltaTime;
            float fade = _oneShotDuration > 0f ? _oneShotTimer / _oneShotDuration : 1f;
            offset += SampleNoise(_oneShotMagnitude * Mathf.Clamp01(fade));
        }

        // Continuous shake — constant magnitude until stopped.
        if (_continuousMagnitude > 0f)
            offset += SampleNoise(_continuousMagnitude);

        transform.localPosition = _originLocalPosition + offset;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>Triggers a one-shot shake. A new call overrides only if the new shake is stronger.</summary>
    public void Shake(ShakeData data)
    {
        if (data.magnitude >= _oneShotMagnitude || _oneShotTimer <= 0f)
        {
            _oneShotMagnitude = data.magnitude;
            _oneShotDuration  = data.duration;
            _oneShotTimer     = data.duration;
        }
    }

    /// <summary>Starts a continuous shake (replaces any current continuous shake).</summary>
    public void StartContinuousShake(ShakeData data)
    {
        _continuousMagnitude = data.magnitude;
    }

    /// <summary>Stops the continuous shake.</summary>
    public void StopContinuousShake()
    {
        _continuousMagnitude = 0f;
    }

    // ── Internal ───────────────────────────────────────────────────────────────

    private Vector3 SampleNoise(float magnitude)
    {
        float t = Time.unscaledTime * 25f;
        float x = (Mathf.PerlinNoise(t + _perlinSeedX, 0f) - 0.5f) * 2f * magnitude;
        float y = (Mathf.PerlinNoise(0f, t + _perlinSeedY) - 0.5f) * 2f * magnitude;
        return new Vector3(x, y, 0f);
    }
}

/// <summary>Shake parameters: how strong and how long.</summary>
[System.Serializable]
public struct ShakeData
{
    [Tooltip("Maximum displacement in world units.")]
    public float magnitude;
    [Tooltip("Duration in seconds. Ignored for continuous shakes.")]
    public float duration;

    public ShakeData(float magnitude, float duration)
    {
        this.magnitude = magnitude;
        this.duration  = duration;
    }
}
