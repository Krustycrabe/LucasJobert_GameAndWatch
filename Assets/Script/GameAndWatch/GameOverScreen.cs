using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Generic game over screen. Listens to the neutral GameOverEvents channel,
/// making it reusable in any level without modification.
/// Each level feeds GameOverEvents through its own bridge script.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Format")]
    [Tooltip("Format du score final. {0} = valeur.")]
    [SerializeField] private string scoreFormat = "Score : {0}";

    [Header("Timing")]
    [Tooltip("Délai en secondes (temps réel) avant d'afficher le panel et de geler le jeu. " +
             "Laisse le temps aux animations de mort de se jouer.")]
    [SerializeField] private float showDelay = 1.2f;

    private void Awake() => panel.SetActive(false);

    private void OnEnable()
    {
        GameOverEvents.OnGameOver     += OnGameOver;
        GameOverEvents.OnScoreUpdated += UpdateFinalScore;
    }

    private void OnDisable()
    {
        GameOverEvents.OnGameOver     -= OnGameOver;
        GameOverEvents.OnScoreUpdated -= UpdateFinalScore;
    }

    private void OnGameOver() => StartCoroutine(ShowGameOverDelayed());

    private IEnumerator ShowGameOverDelayed()
    {
        yield return new WaitForSecondsRealtime(showDelay);
        panel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void UpdateFinalScore(int score) =>
        finalScoreText.text = string.Format(scoreFormat, score);

    /// <summary>Appelé par le bouton Rejouer.</summary>
    public void Restart()
    {
        GameOverEvents.Reset();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Appelé par le bouton Quitter.</summary>
    public void Quit()
    {
        GameOverEvents.Reset();
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
