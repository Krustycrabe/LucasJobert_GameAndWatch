using UnityEngine;

/// <summary>
/// Triggers camera and time-freeze feedback on VirusSplit transitions.
/// Attach to the GameManager (or any active GameObject in Level_FlappyBird).
/// Delegates to the shared singletons: HitStopManager, CameraShake,
/// CameraZoomFeedback, VignetteFeedback.
///
/// Feedback configs are assigned per-event in the Inspector using the
/// shared FeedbackConfigSO ScriptableObject (Create > ShootEmUp > Feedback Config).
/// </summary>
public class VirusSplitFeedback : MonoBehaviour
{
    [Header("Feedback Configs")]
    [Tooltip("Feedback played when the virus splits into two.")]
    [SerializeField] private FeedbackConfigSO splitConfig;

    [Tooltip("Feedback played when the two viruses merge back.")]
    [SerializeField] private FeedbackConfigSO mergeConfig;

    // Static events raised by VirusController — subscribe here to stay decoupled.
    public static System.Action OnSplit;
    public static System.Action OnMerge;

    private void OnEnable()
    {
        OnSplit += HandleSplit;
        OnMerge += HandleMerge;
    }

    private void OnDisable()
    {
        OnSplit -= HandleSplit;
        OnMerge -= HandleMerge;
    }

    private void OnDestroy()
    {
        // Reset static delegates on scene unload to prevent stale subscriptions
        // from accumulating if the component is re-enabled in a subsequent session.
        OnSplit = null;
        OnMerge = null;
    }

    private void HandleSplit() => TriggerFeedback(splitConfig);
    private void HandleMerge() => TriggerFeedback(mergeConfig);

    /// <summary>Runs all feedback effects described by <paramref name="config"/>.</summary>
    private void TriggerFeedback(FeedbackConfigSO config)
    {
        if (config == null) return;

        HitStopManager.Instance?.FreezeFrame(config.hitStopDuration);

        if (config.cameraShake.magnitude > 0f)
            CameraShake.Instance?.Shake(config.cameraShake);

        CameraZoomFeedback.Instance?.Punch(config.zoomAmount, config.zoomDuration);
        VignetteFeedback.Instance?.Flash(config.vignetteIntensity, config.vignetteDuration);
    }
}
