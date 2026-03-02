using TMPro;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [Tooltip("Format d'affichage du score. {0} = valeur.")]
    [SerializeField] private string format = "{0}";

    private void OnEnable() => ScoreManager.OnScoreChanged += UpdateDisplay;
    private void OnDisable() => ScoreManager.OnScoreChanged -= UpdateDisplay;

    private void Start() => UpdateDisplay(0);

    /// <summary>Met à jour le texte du score.</summary>
    private void UpdateDisplay(int score) =>
        scoreText.text = string.Format(format, score);
}
