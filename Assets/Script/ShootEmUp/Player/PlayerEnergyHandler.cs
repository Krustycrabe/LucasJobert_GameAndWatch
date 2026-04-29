using System;
using System.Collections;
using UnityEngine;
using GameAndWatch.Audio;

/// <summary>
/// Tracks collected energy. Activates the Charged trigger on Player_SmUp animator
/// when the threshold is reached, enabling the laser sequence.
/// Fires OnEnergyCollected on each pickup for feedback systems (scale punch, sound, etc.).
/// </summary>
public class PlayerEnergyHandler : MonoBehaviour
{
    private static readonly int ChargedHash = Animator.StringToHash("Charged");

    [SerializeField] private Animator playerAnimator;
    [SerializeField] private int chargeThreshold = 50;

    [Header("Collect Punch")]
    [Tooltip("Transform to scale on collect (typically the player root).")]
    [SerializeField] private Transform punchTarget;
    [Tooltip("Scale overshoot added on each collect (e.g. 0.1 → peaks at 1.1).")]
    [SerializeField] private float punchAmount = 0.1f;
    [Tooltip("Total duration of the scale punch in seconds.")]
    [SerializeField] private float punchDuration = 0.08f;

    /// <summary>Fires each time an energy cell is collected. Carries the collected amount.</summary>
    public event Action<int> OnEnergyCollected;

    private int _currentEnergy;
    private bool _isCharged;
    private Coroutine _punchCoroutine;

    public bool IsCharged => _isCharged;

    /// <summary>Adds energy from a collected cell. Fires Charged trigger when threshold is met.</summary>
    public void CollectEnergy(int amount)
    {
        OnEnergyCollected?.Invoke(amount);
        PlayCollectPunch();
        AudioManager.Instance?.PlayOneShot(SoundIds.ShootEmUp.EnergyCollect);

        if (_isCharged) return;

        _currentEnergy += amount;
        if (_currentEnergy >= chargeThreshold)
        {
            _isCharged = true;
            playerAnimator.SetTrigger(ChargedHash);
            AudioManager.Instance?.PlayOneShot(SoundIds.ShootEmUp.LaserReady);
        }
    }

    /// <summary>Resets energy after the laser is fired. Called by PlayerShooter.</summary>
    public void ConsumeCharge()
    {
        _currentEnergy = 0;
        _isCharged = false;
    }

    private void PlayCollectPunch()
    {
        if (punchTarget == null) return;
        if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
        _punchCoroutine = StartCoroutine(CollectPunchCoroutine());
    }

    private IEnumerator CollectPunchCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            // Sin curve: scale up then back to 1.
            float scale = 1f + punchAmount * Mathf.Sin(t * Mathf.PI);
            punchTarget.localScale = Vector3.one * scale;
            yield return null;
        }
        punchTarget.localScale = Vector3.one;
        _punchCoroutine = null;
    }
}
