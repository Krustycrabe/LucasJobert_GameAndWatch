using System;
using System.Collections.Generic;

/// <summary>
/// Modèle de données sérialisé en JSON sur le disque.
/// Ajoute ici de nouveaux champs pour étendre la sauvegarde.
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>Nom saisi par le joueur lors du tutoriel d'intro.</summary>
    public string playerName = string.Empty;

    /// <summary>True si la séquence d'intro zombie a été complétée au moins une fois.</summary>
    public bool tutorialCompleted = false;

    /// <summary>État courant du menu principal.</summary>
    public MainMenuState mainMenuState = MainMenuState.TutorialPending;

    /// <summary>Snapshot de l'état du menu capturé juste avant le lancement d'un mini-jeu.</summary>
    public MiniGameMenuSnapshot preGameSnapshot = null;

    /// <summary>Valeurs libres extensibles sans modifier cette classe.</summary>
    public SerializableDictionary extras = new SerializableDictionary();
}

// ── Enum ─────────────────────────────────────────────────────────────────────

public enum MainMenuState
{
    TutorialPending  = 0,
    Default          = 1,
    ReturnFromMiniGame = 2,
}

// ── Snapshot ─────────────────────────────────────────────────────────────────

/// <summary>
/// État de chaque GO du menu sauvegardé avant le lancement d'un mini-jeu.
/// Peuplé automatiquement par <see cref="MiniGameMenuSnapshotHandler"/>.
/// </summary>
[Serializable]
public class MiniGameMenuSnapshot
{
    /// <summary>Nom de la scène mini-jeu qui a été chargée.</summary>
    public string loadedMiniGameScene = string.Empty;

    /// <summary>
    /// Liste des états de GO capturés.
    /// Chaque entrée contient le nom du GO, son état actif,
    /// et l'état Animator à rejouer lors de la restauration.
    /// </summary>
    public List<GoSnapshotData> goStates = new List<GoSnapshotData>();
}

/// <summary>Données sérialisées d'un seul GO pour le snapshot.</summary>
[Serializable]
public class GoSnapshotData
{
    /// <summary>Nom du GO — utilisé comme clé pour retrouver l'entrée lors de la restauration.</summary>
    public string goName = string.Empty;

    /// <summary>État actif du GO au moment de la capture.</summary>
    public bool activeSelf = false;

    /// <summary>
    /// Nom de l'état Animator à rejouer lors de la restauration.
    /// Copié depuis SnapshotEntry.restoreAnimatorState au moment de la capture,
    /// afin d'être persisté dans le JSON et disponible même si l'Inspector change.
    /// </summary>
    public string restoreAnimatorState = string.Empty;
}

// ── Dictionnaire sérialisable ─────────────────────────────────────────────────

/// <summary>Dictionnaire string→string compatible JsonUtility via listes parallèles.</summary>
[Serializable]
public class SerializableDictionary
{
    public List<string> keys   = new List<string>();
    public List<string> values = new List<string>();

    public void Set(string key, string value)
    {
        int index = keys.IndexOf(key);
        if (index >= 0) values[index] = value;
        else { keys.Add(key); values.Add(value); }
    }

    public string Get(string key)
    {
        int index = keys.IndexOf(key);
        return index >= 0 ? values[index] : null;
    }

    public bool ContainsKey(string key) => keys.Contains(key);

    public bool Remove(string key)
    {
        int index = keys.IndexOf(key);
        if (index < 0) return false;
        keys.RemoveAt(index);
        values.RemoveAt(index);
        return true;
    }
}
