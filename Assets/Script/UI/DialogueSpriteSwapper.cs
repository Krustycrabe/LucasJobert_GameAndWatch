using UnityEngine;

public class DialogueSpriteSwapper : MonoBehaviour
{
    [Header("Dialogue Trigger")]
    [Tooltip("Index du dialogue (0-based) qui déclenche le switch de sprite.")]
    [SerializeField] private int triggerDialogueIndex = 2;

    [Header("Sprites à switcher")]
    [SerializeField] private GameObject zombieBasePos;
    [SerializeField] private GameObject zombieBasePos2;

    [Header("Animation de fin de dialogues")]
    [SerializeField] private Animator targetAnimator;
    [Tooltip("Nom exact du trigger dans l'Animator Controller.")]
    [SerializeField] private string endAnimationTrigger = "PlayEnd";

    private void OnEnable()
    {
        DialogueAnimator.OnDialogueChanged += HandleDialogueChanged;
        DialogueAnimator.OnDialoguesComplete += HandleDialoguesComplete;
    }

    private void OnDisable()
    {
        DialogueAnimator.OnDialogueChanged -= HandleDialogueChanged;
        DialogueAnimator.OnDialoguesComplete -= HandleDialoguesComplete;
    }

    private void HandleDialogueChanged(int index)
    {
        if (index == triggerDialogueIndex)
        {
            zombieBasePos.SetActive(false);
            zombieBasePos2.SetActive(true);
        }
    }

    private void HandleDialoguesComplete()
    {
        if (targetAnimator != null)
            targetAnimator.SetTrigger(endAnimationTrigger);
    }
}
