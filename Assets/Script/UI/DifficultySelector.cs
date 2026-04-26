using TMPro;
using UnityEngine;

/// <summary>
/// Gère uniquement la sélection cyclique de difficulté pour un mini-jeu.
/// Ne charge aucune scène — le chargement est délégué à MiniGameMenuSnapshotHandler.CaptureAndLaunch().
///
/// BOUTON PLAY (onClick) :
///   Event 1 (optionnel) : DifficultySelector.ApplyCurrentDifficulty()
///   Event 2             : MiniGameMenuSnapshotHandler.CaptureAndLaunch("NomDeLaScene")
/// </summary>
public class DifficultySelector : MonoBehaviour
{
    [Header("Difficultés")]
    [SerializeField] private DifficultyData[] difficulties;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI difficultyText;

    [Header("Animation")]
    [SerializeField] private Animator menuAnimator;
    [Tooltip("Nom du trigger déclenché à chaque changement de difficulté.")]
    [SerializeField] private string changeTrigger = "Change";

    private int _currentIndex;

    private void Start()
    {
        _currentIndex = 0;
        RefreshDisplay();
    }

    /// <summary>Sélectionne la difficulté suivante. Brancher sur ButtonNextDifficulty.OnClick.</summary>
    public void SelectNext()
    {
        Debug.Log("pressed");
        _currentIndex = (_currentIndex + 1) % difficulties.Length;
        RefreshDisplay();
        TriggerAnimation();
    }

    /// <summary>Sélectionne la difficulté précédente. Brancher sur ButtonPreviousDifficulty.OnClick.</summary>
    public void SelectPrevious()
    {
        _currentIndex = (_currentIndex - 1 + difficulties.Length) % difficulties.Length;
        RefreshDisplay();
        TriggerAnimation();
    }

    /// <summary>
    /// Applique la difficulté courante au DifficultyManager.
    /// Brancher sur le bouton Play en Event 1 (optionnel).
    /// Si aucune difficulté n'est configurée, ne fait rien — le jeu peut tout de même se lancer.
    /// </summary>
    public void ApplyCurrentDifficulty()
    {
        if (difficulties.Length == 0) return;
        DifficultyManager.Instance?.SelectDifficulty(difficulties[_currentIndex]);
        Debug.Log($"[DifficultySelector] Difficulté appliquée : '{difficulties[_currentIndex].difficultyName}'.");
    }

    private void RefreshDisplay()
    {
        if (difficulties.Length == 0) return;

        DifficultyData current = difficulties[_currentIndex];

        if (difficultyText != null)
            difficultyText.text = current.difficultyName;

        DifficultyManager.Instance?.SelectDifficulty(current);
    }

    private void TriggerAnimation()
    {
        if (menuAnimator != null && !string.IsNullOrEmpty(changeTrigger))
            menuAnimator.SetTrigger(changeTrigger);
    }
}
