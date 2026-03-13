using TMPro;
using UnityEngine;

/// <summary>
/// Displays the distance score in metres on a TextMeshPro label.
/// Listens to VirusScoreManager.OnMetresChanged.
/// </summary>
public class VirusScoreUI : MonoBehaviour
{
    [SerializeField] private VirusScoreManager scoreManager;
    [SerializeField] private TextMeshProUGUI   scoreText;
    [Tooltip("Format string for the score. {0} = metres. Example: '{0} m'")]
    [SerializeField] private string            scoreFormat = "{0} m";

    private void OnEnable()
    {
        if (scoreManager != null)
            scoreManager.OnMetresChanged += UpdateDisplay;
    }

    private void OnDisable()
    {
        if (scoreManager != null)
            scoreManager.OnMetresChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(int metres)
    {
        if (scoreText != null)
            scoreText.text = string.Format(scoreFormat, metres);
    }
}
