using System.Collections;
using UnityEngine;

/// <summary>
/// Manages shooting and laser states. Drives both Animator controllers.
/// Laser is only available when PlayerEnergyHandler reports IsCharged = true.
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator shootAnimator;
    [SerializeField] private BulletSpawner bulletSpawner;
    [SerializeField] private PlayerEnergyHandler energyHandler;
    [Tooltip("LaserHitbox component on the laser child GameObject. Enabled only during the laser.")]
    [SerializeField] private LaserHitbox laserHitbox;

    [Header("Laser")]
    [SerializeField] private float laserDuration = 2f;

    private static readonly int IsShootingHash = Animator.StringToHash("IsShooting");
    private static readonly int LaserTriggeredHash = Animator.StringToHash("LaserTriggered");
    private static readonly int LaserShootHash = Animator.StringToHash("LaserShoot");
    private static readonly int LaserFinishedHash = Animator.StringToHash("LaserFinished");

    private Coroutine _laserCoroutine;
    private bool _isLaserPlaying;
    private bool _isTouching; // tracks whether the finger is currently on screen

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
        _isTouching = true;
        if (_isLaserPlaying) return;

        shootAnimator.SetBool(IsShootingHash, true);
        bulletSpawner.StartShooting();
    }

    private void HandleTouchEnded()
    {
        _isTouching = false;
        if (_isLaserPlaying) return;
        if (!shootAnimator.GetBool(IsShootingHash)) return;

        shootAnimator.SetBool(IsShootingHash, false);
        bulletSpawner.StopShooting();

        bool charged = energyHandler != null && energyHandler.IsCharged;
        if (charged)
        {
            shootAnimator.SetTrigger(LaserTriggeredHash);
            playerAnimator.SetTrigger(LaserShootHash);
            energyHandler.ConsumeCharge();
            _isLaserPlaying = true;
            if (laserHitbox != null) laserHitbox.enabled = true;
            _laserCoroutine = StartCoroutine(LaserDurationRoutine());
        }
    }

    private IEnumerator LaserDurationRoutine()
    {
        yield return new WaitForSeconds(laserDuration);

        if (laserHitbox != null) laserHitbox.enabled = false;
        shootAnimator.SetTrigger(LaserFinishedHash);
        playerAnimator.SetTrigger(LaserFinishedHash);
        _isLaserPlaying = false;
        _laserCoroutine = null;

        // If the finger is still on screen, resume shooting without requiring a new tap.
        if (_isTouching)
        {
            shootAnimator.SetBool(IsShootingHash, true);
            bulletSpawner.StartShooting();
        }
    }

    private void SetLaserFinished(bool value)
    {
        shootAnimator.SetBool(LaserFinishedHash, value);
        playerAnimator.SetBool(LaserFinishedHash, value);
    }
}
