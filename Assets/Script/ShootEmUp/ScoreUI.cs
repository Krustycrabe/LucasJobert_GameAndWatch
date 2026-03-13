using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Drives the score TextMeshPro display.
/// - Rolling counter: the displayed score counts up 1 by 1 toward the actual score,
///   at a speed proportional to the gap so large bonuses resolve faster.
/// - Punch animation: a sine-curve scale pop on the text, intensity scales with bonus size.
/// - Camera shake: small shake on large bonuses, intensity scales with bonus size.
/// Place this component on the ScorePanel (or any persistent UI parent).
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [SerializeField] private GameDataSO gameData;
    [SerializeField] private SmUpScoreManager scoreManager;
    [SerializeField] private TextMeshProUGUI scoreText;

    private int _displayedScore;
    private float _displayAccumulator;
    private Coroutine _punchCoroutine;

    private void OnEnable()
    {
        scoreManager.OnInstantScoreAdded += HandleInstantScoreAdded;
    }

    private void OnDisable()
    {
        scoreManager.OnInstantScoreAdded -= HandleInstantScoreAdded;
    }

    private void Update()
    {
        AdvanceRollingCounter();
    }

    // ── Rolling counter ────────────────────────────────────────────────────────

    /// <summary>
    /// Each frame, the displayed value counts up toward the actual score 1 point at a time.
    /// Speed = max(minRate, gap × speedFactor) — so large gaps resolve quickly.
    /// </summary>
    private void AdvanceRollingCounter()
    {
        int actual = scoreManager.ActualScore;
        if (_displayedScore >= actual) return;

        int gap  = actual - _displayedScore;
        float rate = Mathf.Max(gameData.minCountupRate, gap * gameData.countupSpeedFactor);
        _displayAccumulator += rate * Time.deltaTime;

        int add = Mathf.FloorToInt(_displayAccumulator);
        if (add <= 0) return;

        _displayedScore     = Mathf.Min(_displayedScore + add, actual);
        _displayAccumulator -= add;
        scoreText.text = _displayedScore.ToString();
    }

    // ── Punch animation ────────────────────────────────────────────────────────

    private void HandleInstantScoreAdded(int amountAdded, int totalScore)
    {
        if (amountAdded < gameData.punchThreshold) return;

        float t = Mathf.InverseLerp(gameData.punchThreshold, gameData.punchMaxGain, amountAdded);

        // Camera shake
        float shakeMag = t * gameData.maxScoreShake;
        if (shakeMag > 0f && CameraShake.Instance != null)
            CameraShake.Instance.Shake(new ShakeData(shakeMag, gameData.punchDuration));

        // Scale punch
        float punchAmount = t * gameData.maxScorePunch;
        if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
        _punchCoroutine = StartCoroutine(PunchAnimation(punchAmount));
    }

    /// <summary>Scales the score text up and back down via a sine curve over punchDuration.</summary>
    private IEnumerator PunchAnimation(float punchAmount)
    {
        float elapsed = 0f;
        while (elapsed < gameData.punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / gameData.punchDuration;
            scoreText.transform.localScale = Vector3.one * (1f + punchAmount * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        scoreText.transform.localScale = Vector3.one;
        _punchCoroutine = null;
    }
}
