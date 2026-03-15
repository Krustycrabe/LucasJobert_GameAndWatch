using System.Collections.Generic;
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

    // ── Registry statique ─────────────────────────────────────────────────────
    // Permet d'appeler PlayPresetOn("NomDuGO", index) depuis n'importe quel
    // Animation Event ou script sans référence directe.

    private static readonly Dictionary<string, AnimationTriggerController> Registry
        = new Dictionary<string, AnimationTriggerController>();

    private void Awake()
    {
        if (targetAnimator == null)
            targetAnimator = GetComponent<Animator>();

        Registry[gameObject.name] = this;
    }

    private void OnDestroy()
    {
        if (Registry.TryGetValue(gameObject.name, out var stored) && stored == this)
            Registry.Remove(gameObject.name);
    }

    // ── Events dialogue ───────────────────────────────────────────────────────

    private void OnEnable()
    {
        DialogueSystem.OnDialogueChanged  += OnDialogueChanged;
        DialogueSystem.OnDialoguesComplete += OnDialoguesComplete;
    }

    private void OnDisable()
    {
        DialogueSystem.OnDialogueChanged  -= OnDialogueChanged;
        DialogueSystem.OnDialoguesComplete -= OnDialoguesComplete;
    }

    private void OnDialogueChanged(int index) => Evaluate(index);
    private void OnDialoguesComplete()        => Evaluate(-1);

    private void Evaluate(int index)
    {
        foreach (AnimationEntry entry in dialogueEntries)
        {
            if (entry.dialogueIndex != index) continue;
            ApplyEntry(entry.parameterName, entry.parameterType, entry.boolValue, entry.floatValue, entry.intValue);
        }
    }

    // ── API publique locale ───────────────────────────────────────────────────

    /// <summary>Déclenche un preset par son index. Branchable sur un bouton OnClick.</summary>
    public void PlayPreset(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= presets.Length)
        {
            Debug.LogWarning($"[AnimationTriggerController] Preset {presetIndex} invalide sur '{gameObject.name}'.");
            return;
        }

        AnimationPreset p = presets[presetIndex];
        ApplyEntry(p.parameterName, p.parameterType, p.boolValue, p.floatValue, p.intValue);
    }

    public void SetTrigger(string paramName)              => targetAnimator.SetTrigger(paramName);
    public void SetBool(string paramName, bool value)     => targetAnimator.SetBool(paramName, value);
    public void SetFloat(string paramName, float value)   => targetAnimator.SetFloat(paramName, value);
    public void SetInt(string paramName, int value)       => targetAnimator.SetInteger(paramName, value);
    public void Play(string stateName)                    => targetAnimator.Play(stateName);
    public void ResetToDefault() { targetAnimator.Rebind(); targetAnimator.Update(0f); }

    private void ApplyEntry(string paramName, ParameterType type, bool boolVal, float floatVal, int intVal)
    {
        switch (type)
        {
            case ParameterType.Trigger: targetAnimator.SetTrigger(paramName);          break;
            case ParameterType.Bool:    targetAnimator.SetBool(paramName, boolVal);    break;
            case ParameterType.Float:   targetAnimator.SetFloat(paramName, floatVal);  break;
            case ParameterType.Int:     targetAnimator.SetInteger(paramName, intVal);  break;
        }
    }

    // ── API statique (Animation Events cross-GO) ──────────────────────────────

    /// <summary>
    /// Déclenche un preset sur un AnimationTriggerController enregistré par nom de GO.
    /// Appelle cette méthode depuis un Animation Event sur n'importe quel Animator.
    /// Le paramètre string doit être au format "NomDuGO:index", ex : "BackGroundManager:0".
    /// </summary>
    public void PlayPresetOn(string targetAndIndex)
    {
        int separator = targetAndIndex.LastIndexOf(':');
        if (separator < 0 || !int.TryParse(targetAndIndex.Substring(separator + 1), out int index))
        {
            Debug.LogWarning($"[AnimationTriggerController] Format invalide : '{targetAndIndex}'. Attendu : 'NomDuGO:index'.");
            return;
        }

        string targetName = targetAndIndex.Substring(0, separator);

        if (!Registry.TryGetValue(targetName, out AnimationTriggerController target))
        {
            Debug.LogWarning($"[AnimationTriggerController] '{targetName}' introuvable dans le registry.");
            return;
        }

        target.PlayPreset(index);
    }
}
