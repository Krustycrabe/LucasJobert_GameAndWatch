using UnityEngine;

public class AnimationTriggerController : MonoBehaviour
{
    [System.Serializable]
    public struct AnimationEntry
    {
        [Tooltip("Index du dialogue déclencheur. -1 = OnDialoguesComplete.")]
        public int dialogueIndex;

        [Tooltip("Nom du paramètre dans l'Animator Controller.")]
        public string parameterName;

        public ParameterType parameterType;
        public bool boolValue;
        public float floatValue;
        public int intValue;
    }

    public enum ParameterType { Trigger, Bool, Float, Int }

    [SerializeField] private Animator targetAnimator;

    [SerializeField] private AnimationEntry[] dialogueEntries;

    private void Awake()
    {
        if (targetAnimator == null)
            targetAnimator = GetComponent<Animator>();
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
        foreach (AnimationEntry entry in dialogueEntries)
        {
            if (entry.dialogueIndex != index) continue;
            Apply(entry);
        }
    }

    private void Apply(AnimationEntry entry)
    {
        switch (entry.parameterType)
        {
            case ParameterType.Trigger: targetAnimator.SetTrigger(entry.parameterName); break;
            case ParameterType.Bool: targetAnimator.SetBool(entry.parameterName, entry.boolValue); break;
            case ParameterType.Float: targetAnimator.SetFloat(entry.parameterName, entry.floatValue); break;
            case ParameterType.Int: targetAnimator.SetInteger(entry.parameterName, entry.intValue); break;
        }
    }

    // Méthodes publiques génériques — appelables depuis n'importe quel script
    public void SetTrigger(string paramName) => targetAnimator.SetTrigger(paramName);
    public void SetBool(string paramName, bool value) => targetAnimator.SetBool(paramName, value);
    public void SetFloat(string paramName, float value) => targetAnimator.SetFloat(paramName, value);
    public void SetInt(string paramName, int value) => targetAnimator.SetInteger(paramName, value);
    public void Play(string stateName) => targetAnimator.Play(stateName);
    public void ResetToDefault() { targetAnimator.Rebind(); targetAnimator.Update(0f); }
}
