using TMPro;
using UnityEngine;

public class MultiplierDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI multiplierText;
    [Tooltip("Format d'affichage du multiplicateur. {0} = valeur.")]
    [SerializeField] private string format = "x{0}";

    private void OnEnable() => ScoreManager.OnMultiplierChanged += UpdateDisplay;
    private void OnDisable() => ScoreManager.OnMultiplierChanged -= UpdateDisplay;

    private void Start() => UpdateDisplay(1);

    /// <summary>Met à jour le texte du multiplicateur.</summary>
    private void UpdateDisplay(int multiplier) =>
        multiplierText.text = string.Format(format, multiplier);
}
