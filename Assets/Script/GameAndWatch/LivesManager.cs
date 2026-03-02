using System;
using UnityEngine;

public class LivesManager : MonoBehaviour
{
    [SerializeField] private int maxLives = 3;

    public static event Action<int> OnLifeLost;
    public static event Action OnPlayerReset;
    public static event Action OnPlayerNeedsReset; // nouveau : délégué à PlayerDeathAnimator
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
        PlayerController.OnPlayerReachedHeart += HandleHeartReached;
    }

    private void OnDisable()
    {
        CollisionDetector.OnPlayerHit -= HandleHit;
        PlayerController.OnPlayerReachedHeart -= HandleHeartReached;
    }

    private void HandleHit()
    {
        _currentLives--;
        OnLifeLost?.Invoke(_currentLives);

        if (_currentLives <= 0) OnGameOver?.Invoke();
        else OnPlayerNeedsReset?.Invoke(); // animation d'abord, reset ensuite
    }

    private void HandleHeartReached() => OnPlayerReset?.Invoke();

    /// <summary>Déclenche OnPlayerReset — appelé par PlayerDeathAnimator après l'animation.</summary>
    public static void FirePlayerReset() => OnPlayerReset?.Invoke();
}
