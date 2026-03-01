using System;
using UnityEngine;

public class LivesManager : MonoBehaviour
{
    [SerializeField] private int maxLives = 3;

    public static event Action<int> OnLifeLost;
    public static event Action OnPlayerReset;
    public static event Action OnGameOver;

    private int _currentLives;

    private void Awake() => _currentLives = maxLives;

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
        else OnPlayerReset?.Invoke();
    }

    private void HandleHeartReached() => OnPlayerReset?.Invoke();
}
