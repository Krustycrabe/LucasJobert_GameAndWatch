using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class DifficultyMenuController : MonoBehaviour
{
    [Header("Difficultés disponibles")]
    [SerializeField] private DifficultyData[] difficulties;

    [Header("Scène de jeu")]
    [SerializeField] private string gameSceneName = "Level_GameAndWatch";

    [Header("UI (optionnel)")]
    [Tooltip("Texte affichant la difficulté sélectionnée.")]
    [SerializeField] private TextMeshProUGUI selectedDifficultyText;

    private DifficultyData _selectedDifficulty;

    private void Start()
    {
        if (difficulties.Length > 0)
            ApplySelection(difficulties[0]);
    }

    /// <summary>Sélectionne une difficulté par index. Branchable sur un Button.OnClick.</summary>
    public void SelectDifficulty(int index)
    {
        if (index < 0 || index >= difficulties.Length)
        {
            Debug.LogWarning($"[DifficultyMenuController] Index invalide : {index}");
            return;
        }
        ApplySelection(difficulties[index]);
    }

    /// <summary>Lance la partie avec la difficulté sélectionnée.</summary>
    public void StartGame()
    {
        if (_selectedDifficulty == null) return;
        DifficultyManager.Instance.SelectDifficulty(_selectedDifficulty);
        SceneManager.LoadScene(gameSceneName);
    }

    private void ApplySelection(DifficultyData difficulty)
    {
        _selectedDifficulty = difficulty;
        if (selectedDifficultyText != null)
            selectedDifficultyText.text = difficulty.difficultyName;
    }
}
