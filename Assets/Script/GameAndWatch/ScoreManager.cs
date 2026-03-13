using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private ScoreData scoreData;

    public static event Action<int> OnScoreChanged;       // score total
    public static event Action<int> OnMultiplierChanged;  // multiplicateur courant

    private void Awake() => scoreData.Reset();

    private void OnEnable()
    {
        PlayerController.OnPlayerReachedHeart += HandleHeartReached;
        CollisionDetector.OnPlayerHit += HandlePlayerHit;
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerReachedHeart -= HandleHeartReached;
        CollisionDetector.OnPlayerHit -= HandlePlayerHit;
    }

    private void HandleHeartReached()
    {
        scoreData.currentScore += scoreData.basePointsPerHeart * scoreData.currentMultiplier;
        scoreData.currentMultiplier += scoreData.multiplierIncrement;

        OnScoreChanged?.Invoke(scoreData.currentScore);
        OnMultiplierChanged?.Invoke(scoreData.currentMultiplier);
        GameOverEvents.RaiseScoreUpdated(scoreData.currentScore);
    }

    private void HandlePlayerHit()
    {
        scoreData.currentMultiplier = 1;
        OnMultiplierChanged?.Invoke(scoreData.currentMultiplier);
    }
}
