using UnityEngine;

public class HeartsDisplay : MonoBehaviour
{
    [Tooltip("Un AnimationTriggerController par cœur, ordonné du dernier au premier : [Life, Life(1), Life(2)].")]
    [SerializeField] private AnimationTriggerController[] heartControllers;

    private const string LoseLifeTrigger = "LoseLife";

    private void Start()
    {
        DifficultyData data = DifficultyManager.Instance?.Current;
        int starting = data != null ? data.startingLives : heartControllers.Length;

        if (starting > heartControllers.Length)
        {
            Debug.LogWarning($"[HeartsDisplay] startingLives ({starting}) > nombre de cœurs UI ({heartControllers.Length}).");
            starting = heartControllers.Length;
        }

        for (int i = 0; i < heartControllers.Length; i++)
            heartControllers[i].gameObject.SetActive(i < starting);
    }

    private void OnEnable() => LivesManager.OnLifeLost += HandleLifeLost;
    private void OnDisable() => LivesManager.OnLifeLost -= HandleLifeLost;

    /// <summary>Déclenche l'animation de disparition sur le cœur correspondant aux vies restantes.</summary>
    private void HandleLifeLost(int remainingLives)
    {
        if (remainingLives < 0 || remainingLives >= heartControllers.Length)
        {
            Debug.LogWarning($"[HeartsDisplay] Index invalide : {remainingLives}");
            return;
        }

        heartControllers[remainingLives].SetTrigger(LoseLifeTrigger);
    }
}
