using UnityEngine;

public class GameObjectActivator : MonoBehaviour
{
    [System.Serializable]
    public struct ActivationEntry
    {
        [Tooltip("Index du dialogue d�clencheur. -1 = OnDialoguesComplete.")]
        public int dialogueIndex;

        [Tooltip("true = activer, false = d�sactiver.")]
        public bool activate;
    }

    [Header("Target")]
    [Tooltip("GameObject � contr�ler. Si vide, cible le GameObject courant.")]
    [SerializeField] private GameObject target;

    [Header("Etat initial")]
    [SerializeField] private bool activeOnStart = true;

    [Header("Dialogue Entries")]
    [SerializeField] private ActivationEntry[] dialogueEntries;

    // When true, LateUpdate forces the target to stay inactive each frame,
    // preventing the Animator's Write Defaults from overriding our SetActive(false).
    private bool _forcedInactive;

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

    private void LateUpdate()
    {
        if (_forcedInactive && target != null && target.activeSelf)
            target.SetActive(false);
    }

    private void OnDialogueChanged(int index) => Evaluate(index);
    private void OnDialoguesComplete() => Evaluate(-1);

    private void Evaluate(int index)
    {
        foreach (ActivationEntry entry in dialogueEntries)
        {
            if (entry.dialogueIndex != index) continue;
            ApplyState(entry.activate);
        }
    }

    private void ApplyState(bool activate)
    {
        _forcedInactive = !activate;
        target.SetActive(activate);
    }

    /// <summary>Active le GameObject cible et lève le verrouillage forcé.</summary>
    public void Activate() => ApplyState(true);

    /// <summary>Désactive le GameObject cible et le verrouille pour résister aux Write Defaults de l'Animator.</summary>
    public void Deactivate() => ApplyState(false);

    /// <summary>Inverse l'état actuel du GameObject cible.</summary>
    public void Toggle() => ApplyState(!target.activeSelf);
}

