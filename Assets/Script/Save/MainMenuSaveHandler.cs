using System.Collections;
using UnityEngine;

/// <summary>
/// Orchestre le save/load côté scène MainMenu.
///
/// RESPONSABILITÉS :
///   - Dans Awake (avant tout Start) : désactive les GO de la séquence tuto
///     si elle a déjà été complétée, empêchant DialogueSystem et
///     AnimationTriggerController de démarrer et de fire leurs events.
///   - Dans Start : positionne les Animators dans le bon état.
///   - Se branche sur NameInputController.OnNameValidated pour sauvegarder le nom.
///   - Expose NotifyTutorialCompleted à appeler depuis TutorialAnimationEventReceiver.
/// </summary>
public class MainMenuSaveHandler : MonoBehaviour
{
    [Header("Animators")]
    [SerializeField] private Animator zombieTutoAnimator;
    [SerializeField] private Animator mainMenuButtonAnimator;

    [Header("GO de la séquence tuto à bloquer lors du skip")]
    [Tooltip("DialogueFrame — enfant de ZombieTutoManager.")]
    [SerializeField] private GameObject dialogueFrame;
    [Tooltip("NameEnterPanel — enfant de ZombieTutoManager.")]
    [SerializeField] private GameObject nameEnterPanel;
    [Tooltip("ButtonParent — contient le bouton NextDialogue, doit être masqué après le tuto.")]
    [SerializeField] private GameObject nextDialogueButtonParent;
    [Tooltip("AnimationTriggerController sur ZombieTutoManager — désactivé pour couper les events dialogue.")]
    [SerializeField] private AnimationTriggerController zombieTutoTriggerController;

    [Header("Paramètres Animator")]
    [Tooltip("Nom du boolean Animator qui transite depuis AnyState vers Idle OUT.")]
    [SerializeField] private string tutorialDoneParameter = "TutoFinished";

    private const string ButtonsIdleState = "MainMenuButtonIN";

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Bloquer la séquence tuto AVANT que les Start() ne s'exécutent.
        // Cela empêche DialogueSystem.Start() de lancer le typewriter et
        // AnimationTriggerController.OnEnable() de s'abonner aux events statiques.
        if (SaveSystem.Instance != null && SaveSystem.Instance.Data.tutorialCompleted)
            BlockTutorialSequenceEarly();
    }

    private void OnEnable()
    {
        NameInputController.OnNameValidated += OnNameValidated;
    }

    private void OnDisable()
    {
        NameInputController.OnNameValidated -= OnNameValidated;
    }

    private void Start()
    {
        if (SaveSystem.Instance == null)
        {
            Debug.LogError("[MainMenuSaveHandler] SaveSystem introuvable.");
            return;
        }

        ApplySavedState(SaveSystem.Instance.Data);
    }

    // ── Blocage anticipé (Awake) ──────────────────────────────────────────────

    /// <summary>
    /// Désactive DialogueFrame, NameEnterPanel et l'AnimationTriggerController du zombie
    /// avant leur Start(), pour éviter tout fire d'event dialogue.
    /// </summary>
    private void BlockTutorialSequenceEarly()
    {
        if (dialogueFrame != null)               dialogueFrame.SetActive(false);
        if (nameEnterPanel != null)              nameEnterPanel.SetActive(false);
        if (nextDialogueButtonParent != null)    nextDialogueButtonParent.SetActive(false);
        if (zombieTutoTriggerController != null) zombieTutoTriggerController.enabled = false;

        Debug.Log("[MainMenuSaveHandler] Séquence tuto bloquée en Awake.");
    }

    // ── Application de l'état sauvegardé (Start) ─────────────────────────────

    private void ApplySavedState(SaveData data)
    {
        switch (data.mainMenuState)
        {
            case MainMenuState.TutorialPending:
                // Rien à faire — la séquence tuto se joue normalement.
                break;

            case MainMenuState.Default:
                SkipTutorialAnimator(data);
                ShowMenuButtons();
                break;

            case MainMenuState.ReturnFromMiniGame:
                SkipTutorialAnimator(data);
                RestorePreGameSnapshot(data.preGameSnapshot);
                break;
        }
    }

    /// <summary>
    /// Positionne l'Animator du zombie en état TutoFinished.
    /// Le blocage des events a déjà été fait dans Awake — ici on gère uniquement l'Animator.
    /// </summary>
    private void SkipTutorialAnimator(SaveData data)
    {
        if (zombieTutoAnimator != null)
            zombieTutoAnimator.SetBool(tutorialDoneParameter, true);

        Debug.Log($"[MainMenuSaveHandler] Animator tuto skippé pour '{data.playerName}'.");
    }

    /// <summary>
    /// Place l'Animator des boutons directement dans l'état idle visible
    /// sans rejouer l'animation d'apparition, puis nettoie le trigger Appear
    /// au frame suivant pour consumer tout trigger résiduel.
    /// </summary>
    private void ShowMenuButtons()
    {
        if (mainMenuButtonAnimator == null) return;

        mainMenuButtonAnimator.Play(ButtonsIdleState, 0, 0f);

        // Nettoie le trigger Appear au frame suivant pour éliminer
        // tout trigger résiduel qui aurait pu être posé avant ce point.
        StartCoroutine(ResetAppearTriggerNextFrame());

        Debug.Log("[MainMenuSaveHandler] Boutons positionnés en état visible.");
    }

    private IEnumerator ResetAppearTriggerNextFrame()
    {
        yield return null;
        if (mainMenuButtonAnimator != null)
            mainMenuButtonAnimator.ResetTrigger("Appear");
    }

    /// <summary>
    /// Restaure le snapshot capturé avant le lancement du mini-jeu.
    /// La coroutine est démarrée sur ce MonoBehaviour (toujours actif),
    /// car le MiniGameMenu peut être inactif au moment du chargement.
    /// </summary>
    private void RestorePreGameSnapshot(MiniGameMenuSnapshot snapshot)
    {
        if (snapshot == null)
        {
            ShowMenuButtons();
            SaveSystem.Instance.ClearPreGameSnapshot();
            return;
        }

        MiniGameMenuSnapshotHandler handler = FindAnyObjectByType<MiniGameMenuSnapshotHandler>();
        if (handler != null)
            StartCoroutine(handler.Restore(snapshot));
        else
            Debug.LogWarning("[MainMenuSaveHandler] MiniGameMenuSnapshotHandler introuvable dans la scène.");
    }

    // ── Callbacks ────────────────────────────────────────────────────────────

    private void OnNameValidated(string playerName)
    {
        SaveSystem.Instance?.SetPlayerName(playerName);
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Appeler depuis TutorialAnimationEventReceiver quand la séquence zombie est terminée.
    /// Sauvegarde la complétion et l'état Default du menu.
    /// </summary>
    public void NotifyTutorialCompleted()
    {
        SaveSystem.Instance?.SetTutorialCompleted();
        Debug.Log("[MainMenuSaveHandler] Tutoriel marqué complété et sauvegardé.");
    }
}
