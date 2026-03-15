using UnityEngine;

/// <summary>
/// Triggers camera and time-freeze feedback on GameAndWatch events.
/// Attach to the GameAndWatchManager (or any active GameObject in Level_GameAndWatch).
/// Delegates to the shared singletons: HitStopManager, CameraShake,
/// CameraZoomFeedback, VignetteFeedback.
///
/// Feedback configs are assigned per-event in the Inspector using the
/// shared FeedbackConfigSO ScriptableObject (Create > ShootEmUp > Feedback Config).
/// </summary>
public class GameAndWatchFeedback : MonoBehaviour
{
    [Header("Feedback Configs")]
    [Tooltip("Feedback played when the player collides with a globule.")]
    [SerializeField] private FeedbackConfigSO globuleHitConfig;

    [Tooltip("Feedback played when the player reaches the heart column.")]
    [SerializeField] private FeedbackConfigSO heartReachedConfig;

    private void OnEnable()
    {
        CollisionDetector.OnPlayerHit       += HandleGlobuleHit;
        PlayerController.OnPlayerReachedHeart += HandleHeartReached;
    }

    private void OnDisable()
    {
        CollisionDetector.OnPlayerHit         -= HandleGlobuleHit;
        PlayerController.OnPlayerReachedHeart -= HandleHeartReached;
    }

    private void HandleGlobuleHit()    => TriggerFeedback(globuleHitConfig);
    private void HandleHeartReached()  => TriggerFeedback(heartReachedConfig);

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
