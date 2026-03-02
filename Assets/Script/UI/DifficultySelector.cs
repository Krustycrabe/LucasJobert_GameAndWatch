using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Scène")]
    [SerializeField] private string gameSceneName = "Level_GameAndWatch";

    private int _currentIndex;

    private const int WrapMin = 0;

    private void Start()
    {
        _currentIndex = 1;
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

    /// <summary>Lance la partie avec la difficulté active. Brancher sur le bouton Play.</summary>
    public void StartGame()
    {
        if (difficulties.Length == 0) return;

        DifficultyManager.Instance.SelectDifficulty(difficulties[_currentIndex]);
        SceneManager.LoadScene(gameSceneName);
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
