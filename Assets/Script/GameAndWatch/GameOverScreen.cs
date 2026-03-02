using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Format")]
    [Tooltip("Format du score final. {0} = valeur.")]
    [SerializeField] private string scoreFormat = "Score : {0}";

    private void Awake() => panel.SetActive(false);

    private void OnEnable()
    {
        PlayerHitAnimTrigger.OnDeathAnimationComplete += ShowGameOver;
        ScoreManager.OnScoreChanged += UpdateFinalScore;
    }

    private void OnDisable()
    {
        PlayerHitAnimTrigger.OnDeathAnimationComplete -= ShowGameOver;
        ScoreManager.OnScoreChanged -= UpdateFinalScore;
    }

    private void ShowGameOver()
    {
        panel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void UpdateFinalScore(int score) =>
        finalScoreText.text = string.Format(scoreFormat, score);

    /// <summary>Appelé par le bouton Rejouer.</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Appelé par le bouton Quitter.</summary>
    public void Quit()
    {
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }
    }
}
