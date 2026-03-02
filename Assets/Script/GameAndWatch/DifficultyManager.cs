using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Tooltip("Difficulté appliquée si aucune n'est sélectionnée depuis le menu.")]
    [SerializeField] private DifficultyData defaultDifficulty;

    public DifficultyData Current { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Current = defaultDifficulty;
    }

    /// <summary>Sélectionne la difficulté depuis le menu principal.</summary>
    public void SelectDifficulty(DifficultyData difficulty)
    {
        Current = difficulty;
        Debug.Log($"[DifficultyManager] Difficulté sélectionnée : {difficulty.difficultyName}");
    }
}
