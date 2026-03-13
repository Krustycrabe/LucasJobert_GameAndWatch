using UnityEngine;

/// <summary>
/// Listens to PlayerHealth.OnDead, triggers the "OnDeath" animation parameter
/// and disables movement and shooting for the death sequence.
/// The trigger must exist in the Player_SmUp AnimatorController (Any State transition).
/// </summary>
public class PlayerDeathAnimator : MonoBehaviour
{
    private static readonly int OnDeathHash = Animator.StringToHash("OnDeath");

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerShooter playerShooter;

    private void OnEnable()  => playerHealth.OnDead += HandleDead;
    private void OnDisable() => playerHealth.OnDead -= HandleDead;

    private void HandleDead()
    {
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerShooter  != null) playerShooter.enabled  = false;

        playerAnimator.SetTrigger(OnDeathHash);
    }
}
