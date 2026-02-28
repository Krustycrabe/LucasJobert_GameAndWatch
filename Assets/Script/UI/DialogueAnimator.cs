using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text dialogueText;

    [Header("Dialogues")]
    [SerializeField] private string[] dialogues;

    [Header("Settings")]
    [Tooltip("Nombre de caractères révélés par seconde.")]
    [SerializeField] private float charsPerSecond = 20f;

    public static event Action<int> OnDialogueChanged;
    public static event Action OnDialoguesComplete;

    private Coroutine _typingCoroutine;
    private int _currentDialogueIndex = -1;

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
    /// Si l'animation est en cours, la saute. Sinon, passe au dialogue suivant.
    /// À brancher sur le ButtonNextDialogue.
    /// </summary>
    public void OnNextPressed()
    {
        if (IsAnimating)
        {
            SkipAnimation();
            return;
        }

        int nextIndex = _currentDialogueIndex + 1;

        if (nextIndex < dialogues.Length)
        {
            ShowDialogue(nextIndex);
        }
        else
        {
            OnDialoguesComplete?.Invoke();
        }
    }

    /// <summary>
    /// Affiche un dialogue précis via son index et lance l'animation typewriter.
    /// </summary>
    public void ShowDialogue(int index)
    {
        if (index < 0 || index >= dialogues.Length)
        {
            Debug.LogWarning($"[DialogueAnimator] Index {index} hors du tableau ({dialogues.Length} dialogues).");
            return;
        }

        _currentDialogueIndex = index;
        OnDialogueChanged?.Invoke(_currentDialogueIndex);
        SetText(dialogues[index]);
    }

    /// <summary>
    /// Saute l'animation et révèle immédiatement tous les caractères.
    /// </summary>
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
    public bool IsLastDialogue => _currentDialogueIndex >= dialogues.Length - 1 && !IsAnimating;

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
    }
}
