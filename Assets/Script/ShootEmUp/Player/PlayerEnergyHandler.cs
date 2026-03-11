using UnityEngine;

/// <summary>
/// Tracks collected energy. Activates the Charged trigger on Player_SmUp animator
/// when the threshold is reached, enabling the laser sequence.
/// </summary>
public class PlayerEnergyHandler : MonoBehaviour
{
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private int chargeThreshold = 50;

    private static readonly int ChargedHash = Animator.StringToHash("Charged");

    private int _currentEnergy;
    private bool _isCharged;

    public bool IsCharged => _isCharged;

    /// <summary>Adds energy from a collected cell. Fires Charged trigger when threshold is met.</summary>
    public void CollectEnergy(int amount)
    {
        if (_isCharged) return;

        _currentEnergy += amount;
        if (_currentEnergy >= chargeThreshold)
        {
            _isCharged = true;
            playerAnimator.SetTrigger(ChargedHash);
        }
    }

    /// <summary>Resets energy after the laser is fired. Called by PlayerShooter.</summary>
    public void ConsumeCharge()
    {
        _currentEnergy = 0;
        _isCharged = false;
    }
}
