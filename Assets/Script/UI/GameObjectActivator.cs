using UnityEngine;

public class GameObjectActivator : MonoBehaviour
{
    [System.Serializable]
    public struct ActivationEntry
    {
        [Tooltip("Index du dialogue déclencheur. -1 = OnDialoguesComplete.")]
        public int dialogueIndex;

        [Tooltip("true = activer, false = désactiver.")]
        public bool activate;
    }

    [Header("Target")]
    [Tooltip("GameObject à contrôler. Si vide, cible le GameObject courant.")]
    [SerializeField] private GameObject target;

    [Header("Etat initial")]
    [SerializeField] private bool activeOnStart = true;

    [Header("Dialogue Entries")]
    [SerializeField] private ActivationEntry[] dialogueEntries;

    private void Awake()
    {
        if (target == null)
            target = gameObject;

        target.SetActive(activeOnStart);
    }

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
        foreach (ActivationEntry entry in dialogueEntries)
        {
            if (entry.dialogueIndex != index) continue;
            target.SetActive(entry.activate);
        }
    }

    /// <summary>Active le GameObject cible.</summary>
    public void Activate() => target.SetActive(true);

    /// <summary>Désactive le GameObject cible.</summary>
    public void Deactivate() => target.SetActive(false);

    /// <summary>Inverse l'état actuel du GameObject cible.</summary>
    public void Toggle() => target.SetActive(!target.activeSelf);
}

