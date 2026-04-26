using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Capture l'état du menu principal avant le lancement d'un mini-jeu
/// et le restaure intégralement au retour.
///
/// WIRING (Inspector) :
///   snapshotEntries → liste de couples [GO cible / état Animator à restaurer].
///   Ajouter ici chaque GO dont l'état (actif + animation) doit être sauvegardé.
///
/// BOUTON PLAY (onClick) :
///   Event 1 (optionnel) : DifficultySelector.ApplyCurrentDifficulty()
///   Event 2             : MiniGameMenuSnapshotHandler.CaptureAndLaunch("NomDeLaScene")
/// </summary>
public class MiniGameMenuSnapshotHandler : MonoBehaviour
{
    // ── Types internes ────────────────────────────────────────────────────────

    [Serializable]
    public class SnapshotEntry
    {
        [Tooltip("GameObject dont l'état actif doit être capturé et restauré.")]
        public GameObject target;

        [Tooltip("Nom exact de l'état Animator à jouer lors de la restauration. " +
                 "Laisser vide si le GO n'a pas d'Animator ou si on ne veut pas le forcer.")]
        public string restoreAnimatorState = string.Empty;
    }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("GO à capturer / restaurer")]
    [Tooltip("Chaque entrée définit un GO à snapshoter et l'état Animator à rejouer au retour.")]
    [SerializeField] private List<SnapshotEntry> snapshotEntries = new List<SnapshotEntry>();

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Capture l'état courant de tous les GO configurés, sauvegarde le snapshot
    /// et charge la scène du mini-jeu.
    /// Event 2 sur le bouton Play — paramètre = nom exact de la scène à charger.
    /// </summary>
    public void CaptureAndLaunch(string sceneName)
    {
        if (SaveSystem.Instance == null)
        {
            Debug.LogError("[MiniGameMenuSnapshotHandler] SaveSystem introuvable.");
            return;
        }

        // On écrase toujours le snapshot précédent pour repartir d'un état propre.
        MiniGameMenuSnapshot snapshot = BuildSnapshot(sceneName);
        SaveSystem.Instance.SavePreGameSnapshot(snapshot);

        Debug.Log($"[MiniGameMenuSnapshotHandler] Snapshot capturé ({snapshot.goStates.Count} GO) — chargement de '{sceneName}'.");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Restaure les états Animator de chaque GO d'après le snapshot,
    /// puis efface le snapshot pour qu'il ne soit plus appliqué aux rechargements suivants.
    /// Appelable depuis n'importe quel MonoBehaviour actif (ex: MainMenuSaveHandler).
    /// Attend un frame avant de jouer les états pour laisser les Animators s'initialiser.
    /// </summary>
    public IEnumerator Restore(MiniGameMenuSnapshot snapshot)
    {
        if (snapshot == null)
        {
            Debug.LogWarning("[MiniGameMenuSnapshotHandler] Snapshot null — restauration ignorée.");
            yield break;
        }

        // Attend un frame pour que tous les Animators aient terminé leur Awake/OnEnable.
        yield return null;

        foreach (GoSnapshotData data in snapshot.goStates)
        {
            if (string.IsNullOrEmpty(data.restoreAnimatorState)) continue;

            SnapshotEntry entry = snapshotEntries.Find(e => e.target != null && e.target.name == data.goName);
            if (entry == null || entry.target == null)
            {
                Debug.LogWarning($"[MiniGameMenuSnapshotHandler] Entrée introuvable pour '{data.goName}'.");
                continue;
            }

            Animator anim = entry.target.GetComponent<Animator>();
            if (anim != null)
            {
                anim.Play(data.restoreAnimatorState, 0, 0f);
                Debug.Log($"[MiniGameMenuSnapshotHandler] '{data.goName}' → état '{data.restoreAnimatorState}'.");
            }
            else
            {
                Debug.LogWarning($"[MiniGameMenuSnapshotHandler] Pas d'Animator sur '{data.goName}'.");
            }
        }

        SaveSystem.Instance?.ClearPreGameSnapshot();
        Debug.Log("[MiniGameMenuSnapshotHandler] Restauration terminée — snapshot effacé.");
    }

    // ── Construction du snapshot ──────────────────────────────────────────────

    private MiniGameMenuSnapshot BuildSnapshot(string sceneName)
    {
        MiniGameMenuSnapshot snapshot = new MiniGameMenuSnapshot
        {
            loadedMiniGameScene = sceneName,
            goStates            = new List<GoSnapshotData>()
        };

        foreach (SnapshotEntry entry in snapshotEntries)
        {
            if (entry.target == null) continue;

            snapshot.goStates.Add(new GoSnapshotData
            {
                goName               = entry.target.name,
                activeSelf           = entry.target.activeSelf,
                // On copie l'état Animator cible dans le JSON pour le persister
                // indépendamment de l'état futur de l'Inspector.
                restoreAnimatorState = entry.restoreAnimatorState,
            });
        }

        return snapshot;
    }
}
