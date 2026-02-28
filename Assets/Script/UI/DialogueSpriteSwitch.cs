using UnityEngine;

public class DialogueSpriteSwitch : MonoBehaviour
{
    [System.Serializable]
    public struct SpriteSwapEntry
    {
        [Tooltip("Index du dialogue déclencheur. -1 = OnDialoguesComplete.")]
        public int dialogueIndex;

        [Tooltip("SpriteRenderer à masquer. Optionnel.")]
        public SpriteRenderer toDisable;

        [Tooltip("SpriteRenderer à afficher. Optionnel.")]
        public SpriteRenderer toEnable;
    }

    [SerializeField] private SpriteSwapEntry[] entries;

    private void OnEnable()
    {
        DialogueSystem.OnDialogueChanged += OnDialogueChanged;
        DialogueSystem.OnDialoguesComplete += OnDialoguesComplete;
    }

    private void OnDisable()
    {
        DialogueSystem.OnDialogueChanged -= OnDialogueChanged;
        DialogueSystem.OnDialoguesComplete -= OnDialoguesComplete;
    }

    private void OnDialogueChanged(int index) => Evaluate(index);

    private void OnDialoguesComplete() => Evaluate(-1);

    private void Evaluate(int index)
    {
        foreach (SpriteSwapEntry entry in entries)
        {
            if (entry.dialogueIndex != index) continue;

            if (entry.toDisable != null) entry.toDisable.enabled = false;
            if (entry.toEnable != null) entry.toEnable.enabled = true;
        }
    }
}
