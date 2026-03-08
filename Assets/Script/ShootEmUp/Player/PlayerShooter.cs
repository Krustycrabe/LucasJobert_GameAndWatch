using System.Collections;
using UnityEngine;

/// <summary>
/// Manages shooting and laser states. Drives both Animator controllers.
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator shootAnimator;
    [SerializeField] private BulletSpawner bulletSpawner;

    [Header("Laser")]
    [SerializeField] private float laserDuration = 2f;

    private static readonly int IsShootingHash = Animator.StringToHash("IsShooting");
    private static readonly int LaserTriggeredHash = Animator.StringToHash("LaserTriggered");
    private static readonly int LaserShootHash = Animator.StringToHash("LaserShoot");
    private static readonly int LaserFinishedHash = Animator.StringToHash("LaserFinished");

    private Coroutine _laserCoroutine;
    private bool _isLaserPlaying;  // verrou pendant l'animation laser

    private void OnEnable()
    {
        inputHandler.OnTouchBegan += HandleTouchBegan;
        inputHandler.OnTouchEnded += HandleTouchEnded;
    }

    private void OnDisable()
    {
        inputHandler.OnTouchBegan -= HandleTouchBegan;
        inputHandler.OnTouchEnded -= HandleTouchEnded;
    }

    private void HandleTouchBegan(Vector2 screenPosition)
    {
        // Shoot interdit pendant le laser
        if (_isLaserPlaying) return;

        // Les deux animators démarrent leur anim de shoot simultanément
        shootAnimator.SetBool(IsShootingHash, true);
        playerAnimator.SetTrigger(LaserShootHash);
        bulletSpawner.StartShooting();
    }

    private void HandleTouchEnded()
    {
        // Si le laser joue déjà ou si on ne tirait pas, on ignore
        if (_isLaserPlaying) return;
        if (!shootAnimator.GetBool(IsShootingHash)) return;

        shootAnimator.SetBool(IsShootingHash, false);
        bulletSpawner.StopShooting();

        shootAnimator.SetTrigger(LaserTriggeredHash);
        playerAnimator.SetTrigger(LaserShootHash);

        _isLaserPlaying = true;
        _laserCoroutine = StartCoroutine(LaserDurationRoutine());
    }

    private IEnumerator LaserDurationRoutine()
    {
        yield return new WaitForSeconds(laserDuration);
        SetLaserFinished(true);
        yield return null;
        SetLaserFinished(false);
        _isLaserPlaying = false;
        _laserCoroutine = null;
    }

    private void SetLaserFinished(bool value)
    {
        shootAnimator.SetBool(LaserFinishedHash, value);
        playerAnimator.SetBool(LaserFinishedHash, value);
    }
}
