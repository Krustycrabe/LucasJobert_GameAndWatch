using UnityEngine;

public class AnimationTriggerController : MonoBehaviour
{
    [System.Serializable]
    public struct AnimationEntry
    {
        [Tooltip("Index du dialogue déclencheur. -1 = OnDialoguesComplete.")]
        public int dialogueIndex;
        public string parameterName;
        public ParameterType parameterType;
        public bool boolValue;
        public float floatValue;
        public int intValue;
    }

    [System.Serializable]
    public struct AnimationPreset
    {
        [Tooltip("Nom du preset, pour s'y retrouver dans l'Inspector.")]
        public string presetName;
        public string parameterName;
        public ParameterType parameterType;
        public bool boolValue;
        public float floatValue;
        public int intValue;
    }

    public enum ParameterType { Trigger, Bool, Float, Int }

    [Header("Animator")]
    [SerializeField] private Animator targetAnimator;

    [Header("Dialogue Entries")]
    [SerializeField] private AnimationEntry[] dialogueEntries;

    [Header("Presets (boutons, events externes...)")]
    [SerializeField] private AnimationPreset[] presets;

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
            ApplyEntry(entry.parameterName, entry.parameterType, entry.boolValue, entry.floatValue, entry.intValue);
        }
    }

    /// <summary>Déclenche un preset par son index. Branchable sur un bouton OnClick.</summary>
    public void PlayPreset(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= presets.Length)
        {
            Debug.LogWarning($"[AnimationTriggerController] Preset {presetIndex} invalide.");
            return;
        }

        AnimationPreset p = presets[presetIndex];
        ApplyEntry(p.parameterName, p.parameterType, p.boolValue, p.floatValue, p.intValue);
    }

    private void ApplyEntry(string paramName, ParameterType type, bool boolVal, float floatVal, int intVal)
    {
        switch (type)
        {
            case ParameterType.Trigger: targetAnimator.SetTrigger(paramName); break;
            case ParameterType.Bool: targetAnimator.SetBool(paramName, boolVal); break;
            case ParameterType.Float: targetAnimator.SetFloat(paramName, floatVal); break;
            case ParameterType.Int: targetAnimator.SetInteger(paramName, intVal); break;
        }
    }

    // Méthodes publiques génériques
    public void SetTrigger(string paramName) => targetAnimator.SetTrigger(paramName);
    public void SetBool(string paramName, bool value) => targetAnimator.SetBool(paramName, value);
    public void SetFloat(string paramName, float value) => targetAnimator.SetFloat(paramName, value);
    public void SetInt(string paramName, int value) => targetAnimator.SetInteger(paramName, value);
    public void Play(string stateName) => targetAnimator.Play(stateName);
    public void ResetToDefault() { targetAnimator.Rebind(); targetAnimator.Update(0f); }
}
