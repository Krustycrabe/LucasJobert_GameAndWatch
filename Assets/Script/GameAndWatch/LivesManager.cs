using System;
using UnityEngine;

public class LivesManager : MonoBehaviour
{
    [SerializeField] private int maxLives = 3;

    public static event Action<int> OnLifeLost;
    public static event Action OnPlayerReset;
    public static event Action OnPlayerNeedsReset; // nouveau : d�l�gu� � PlayerDeathAnimator
    public static event Action OnGameOver;

    private int _currentLives;

    private void Awake()
    {
        DifficultyData data = DifficultyManager.Instance?.Current;
        _currentLives = data != null ? data.startingLives : maxLives;
    }

    private void OnEnable()
    {
        CollisionDetector.OnPlayerHit += HandleHit;
        // OnPlayerReachedHeart is now handled by HeartReachedAnimTrigger, which plays
        // the animation first and then calls FirePlayerReset() when it is done.
        // LivesManager no longer resets the player directly on heart reached.
    }

    private void OnDisable()
    {
        CollisionDetector.OnPlayerHit -= HandleHit;
    }

    private void HandleHit()
    {
        _currentLives--;
        OnLifeLost?.Invoke(_currentLives);

        if (_currentLives <= 0) OnGameOver?.Invoke();
        else OnPlayerNeedsReset?.Invoke();
    }

    /// <summary>D�clenche OnPlayerReset � appel� par PlayerDeathAnimator apr�s l'animation.</summary>
    public static void FirePlayerReset() => OnPlayerReset?.Invoke();
}
