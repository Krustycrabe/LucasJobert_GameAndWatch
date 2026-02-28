using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text dialogueText;

    [Header("Dialogues")]
    [SerializeField] private string[] dialogues;

    [Header("Settings")]
    [SerializeField] private float charsPerSecond = 20f;

    [Tooltip("Index du dernier dialogue actif. -1 = dernier du tableau.")]
    [SerializeField] private int lastDialogueIndex = -1;

    /// <summary>Fired à chaque changement de dialogue. Passe l'index courant.</summary>
    public static event Action<int> OnDialogueChanged;

    /// <summary>Fired automatiquement quand le dernier dialogue est entièrement affiché.</summary>
    public static event Action OnDialoguesComplete;

    private Coroutine _typingCoroutine;
    private int _currentDialogueIndex = -1;
    private bool _sequenceComplete = false;

    private int EffectiveLastIndex => lastDialogueIndex >= 0
        ? Mathf.Min(lastDialogueIndex, dialogues.Length - 1)
        : dialogues.Length - 1;

    private void Awake()
    {
        if (dialogueText == null)
            dialogueText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        if (dialogues.Length > 0)
            ShowDialogue(0);
    }

    /// <summary>
    /// Premier clic : skip le typewriter.
    /// Second clic : dialogue suivant.
    /// </summary>
    public void OnNextPressed()
    {
        if (_sequenceComplete) return;

        if (IsAnimating)
        {
            SkipAnimation();
            return;
        }

        int nextIndex = _currentDialogueIndex + 1;

        if (nextIndex <= EffectiveLastIndex)
            ShowDialogue(nextIndex);
    }

    /// <summary>Affiche un dialogue par son index.</summary>
    public void ShowDialogue(int index)
    {
        if (index < 0 || index >= dialogues.Length)
        {
            Debug.LogWarning($"[DialogueSystem] Index {index} invalide.");
            return;
        }

        _currentDialogueIndex = index;
        OnDialogueChanged?.Invoke(index);
        SetText(dialogues[index]);
    }

    public void SkipAnimation()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        dialogueText.maxVisibleCharacters = dialogueText.text.Length;
    }

    public bool IsAnimating => _typingCoroutine != null;

    private void SetText(string newText)
    {
        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        dialogueText.text = newText;
        dialogueText.maxVisibleCharacters = 0;
        _typingCoroutine = StartCoroutine(TypewriterRoutine());
    }

    private IEnumerator TypewriterRoutine()
    {
        int totalChars = dialogueText.text.Length;
        float delay = 1f / Mathf.Max(charsPerSecond, 0.01f);

        for (int i = 0; i <= totalChars; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }

        _typingCoroutine = null;

        if (_currentDialogueIndex >= EffectiveLastIndex)
        {
            _sequenceComplete = true;
            OnDialoguesComplete?.Invoke();
        }
    }
}
