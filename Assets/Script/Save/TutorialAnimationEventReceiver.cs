using UnityEngine;

/// <summary>
/// Composant à placer sur le même GameObject que l'Animator de "ZombieTutoManager".
///
/// Il reçoit l'Animation Event déclenché à la fin de l'animation de départ du zombie
/// et orchestre deux actions dans le bon ordre :
///   1. Met le boolean Animator <see cref="tutorialDoneParameter"/> à true
///      → déclenche la transition AnyState → Idle OUT.
///   2. Notifie <see cref="MainMenuSaveHandler"/> pour sauvegarder la complétion.
///
/// WIRING (Inspector) :
///   tutorialDoneParameter → nom exact du boolean Animator (ex : "TutoCompleted")
///   saveHandler           → référence au MainMenuSaveHandler de la scène
///
/// ANIMATION EVENT :
///   Sur l'animation de départ du zombie, ajoute un Animation Event à la frame
///   de fin et cible la méthode : OnTutorialSequenceComplete (sans paramètre).
/// </summary>
[RequireComponent(typeof(Animator))]
public class TutorialAnimationEventReceiver : MonoBehaviour
{
    [Header("Animator")]
    [Tooltip("Nom du boolean Animator qui transite depuis AnyState vers Idle OUT.")]
    [SerializeField] private string tutorialDoneParameter = "TutoCompleted";

    [Header("Références scène")]
    [SerializeField] private MainMenuSaveHandler saveHandler;

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (saveHandler == null)
            Debug.LogWarning("[TutorialAnimationEventReceiver] saveHandler non assigné — la sauvegarde ne sera pas déclenchée.");
    }

    // ── Animation Event ───────────────────────────────────────────────────────

    /// <summary>
    /// Appelle cette méthode depuis un Animation Event placé sur la dernière frame
    /// de l'animation de départ du zombie (ex : "ZombieExit" ou "ZombieLeave").
    /// </summary>
    public void OnTutorialSequenceComplete()
    {
        SetTutorialDoneInAnimator();
        NotifySaveSystem();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetTutorialDoneInAnimator()
    {
        if (_animator == null || string.IsNullOrEmpty(tutorialDoneParameter)) return;

        _animator.SetBool(tutorialDoneParameter, true);
        Debug.Log($"[TutorialAnimationEventReceiver] '{tutorialDoneParameter}' → true sur '{gameObject.name}'.");
    }

    private void NotifySaveSystem()
    {
        if (saveHandler != null)
        {
            saveHandler.NotifyTutorialCompleted();
            return;
        }

        // Fallback : cherche le handler dans la scène si non assigné.
        MainMenuSaveHandler found = FindAnyObjectByType<MainMenuSaveHandler>();
        if (found != null)
            found.NotifyTutorialCompleted();
        else
            Debug.LogError("[TutorialAnimationEventReceiver] Impossible de trouver MainMenuSaveHandler dans la scène.");
    }
}
