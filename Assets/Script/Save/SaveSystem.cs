using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Singleton persistant responsable de la lecture et de l'écriture du fichier JSON de sauvegarde.
/// Accéder via <see cref="SaveSystem.Instance"/>.
/// Ne contient aucune logique de jeu — il sérialise et désérialise uniquement <see cref="SaveData"/>.
/// </summary>
public class SaveSystem : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────

    public static SaveSystem Instance { get; private set; }

    // ── Config ───────────────────────────────────────────────────────────────

    private const string SaveFileName = "save.json";

    /// <summary>Chemin absolu du fichier de sauvegarde.</summary>
    public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    // ── État courant ─────────────────────────────────────────────────────────

    /// <summary>Données courantes en mémoire. Toujours non-null après Awake.</summary>
    public SaveData Data { get; private set; }

    /// <summary>Fired après chaque <see cref="Save"/> réussi.</summary>
    public static event Action OnSaved;

    /// <summary>Fired après chaque <see cref="Load"/> réussi.</summary>
    public static event Action OnLoaded;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    // ── API publique ─────────────────────────────────────────────────────────

    /// <summary>
    /// Écrit <see cref="Data"/> dans le fichier JSON sur le disque.
    /// </summary>
    public void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(Data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            OnSaved?.Invoke();
            Debug.Log($"[SaveSystem] Sauvegarde écrite → {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Échec de la sauvegarde : {e.Message}");
        }
    }

    /// <summary>
    /// Charge les données depuis le fichier JSON.
    /// Si le fichier est absent ou corrompu, crée une nouvelle <see cref="SaveData"/>.
    /// </summary>
    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            Data = new SaveData();
            Debug.Log("[SaveSystem] Aucun fichier de sauvegarde trouvé — nouvelle partie.");
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            Data = JsonUtility.FromJson<SaveData>(json);

            if (Data == null)
                Data = new SaveData();

            OnLoaded?.Invoke();
            Debug.Log($"[SaveSystem] Sauvegarde chargée depuis {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Échec du chargement (fichier corrompu ?) : {e.Message}");
            Data = new SaveData();
        }
    }

    /// <summary>
    /// Réinitialise complètement la sauvegarde et écrase le fichier.
    /// </summary>
    public void ResetSave()
    {
        Data = new SaveData();
        Save();
        Debug.Log("[SaveSystem] Sauvegarde réinitialisée.");
    }

    // ── Raccourcis utilitaires ────────────────────────────────────────────────

    /// <summary>Enregistre le nom du joueur et sauvegarde immédiatement.</summary>
    public void SetPlayerName(string playerName)
    {
        Data.playerName = playerName;
        Save();
    }

    /// <summary>Marque le tutoriel comme complété et sauvegarde immédiatement.</summary>
    public void SetTutorialCompleted()
    {
        Data.tutorialCompleted = true;
        Data.mainMenuState     = MainMenuState.Default;
        Save();
    }

    /// <summary>Met à jour l'état du menu principal et sauvegarde immédiatement.</summary>
    public void SetMainMenuState(MainMenuState state)
    {
        Data.mainMenuState = state;
        Save();
    }

    /// <summary>
    /// Enregistre un snapshot de l'état du menu pré-mini-jeu,
    /// bascule l'état en <see cref="MainMenuState.ReturnFromMiniGame"/> et sauvegarde.
    /// </summary>
    public void SavePreGameSnapshot(MiniGameMenuSnapshot snapshot)
    {
        Data.preGameSnapshot = snapshot;
        Data.mainMenuState   = MainMenuState.ReturnFromMiniGame;
        Save();
    }

    /// <summary>
    /// Efface le snapshot pré-mini-jeu, remet l'état en Default et sauvegarde.
    /// </summary>
    public void ClearPreGameSnapshot()
    {
        Data.preGameSnapshot = null;
        Data.mainMenuState   = MainMenuState.Default;
        Save();
    }
}
