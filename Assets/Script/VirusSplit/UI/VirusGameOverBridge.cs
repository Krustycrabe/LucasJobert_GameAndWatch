using UnityEngine;
using GameAndWatch.Audio;

/// <summary>
/// VirusSplit bridge toward the neutral GameOverEvents channel.
/// Mirrors the pattern used in GameOverBridge (ShootEmUp) so the shared
/// GameOverScreen works in this level without any modification.
/// Also resets Time.timeScale to 1 in case a slow-motion effect was active.
/// </summary>
public class VirusGameOverBridge : MonoBehaviour
{
    private bool _raised;

    private void OnEnable()  => GameOverEvents.OnGameOver += HandleGameOver;
    private void OnDisable() => GameOverEvents.OnGameOver -= HandleGameOver;

    private void HandleGameOver()
    {
        if (_raised) return;
        _raised = true;
        AudioManager.Instance?.PlayOneShot(SoundIds.GameAndWatch.GameOver);
        AudioManager.Instance?.StopMusic();
        // Restore time scale — slow motion may have been active at the moment of death.
        Time.timeScale = 1f;
    }
}
