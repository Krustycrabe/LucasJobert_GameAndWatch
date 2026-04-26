using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the name entry panel shown after the intro dialogue.
///
/// FLOW:
///   1. Show() — called from an Animation Event or AnimationTriggerController preset.
///   2. Player types a name → presses validate button or Enter.
///   3. If invalid → show ErrorText, hide Placeholder + NameText.
///   4. If valid   → set NameOk bool + fire NameValidate trigger on targetAnimator,
///                   fire OnNameValidated event, hide panel.
///
/// WIRING (Inspector):
///   nameInputField    → InputFieldName   (TMP_InputField)
///   validateButton    → BTN_ValideName   (Button)
///   placeholderText   → Placeholder      (TMP_Text)
///   nameText          → NameText         (TMP_Text)
///   errorText         → ErrorText        (TMP_Text)
///   panel             → NameEnterPanel   (GameObject)
///   targetAnimator    → ZombieTutoManager Animator
///   nameOkParameter   → "NameOk"         (bool param)
///   nameValidateTrigger → "NameValidate" (trigger param)
///
/// NOTE: remove PlayPreset from BTN_ValideName.onClick — this script handles
///       both the bool and the trigger directly to guarantee correct ordering.
/// </summary>
public class NameInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button         validateButton;
    [SerializeField] private TMP_Text       placeholderText;
    [SerializeField] private TMP_Text       nameText;
    [SerializeField] private TMP_Text       errorText;
    [SerializeField] private GameObject     panel;

    [Header("Animator")]
    [Tooltip("Animator containing NameOk (bool) and NameValidate (trigger).")]
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private string   nameOkParameter     = "NameOk";
    [SerializeField] private string   nameValidateTrigger = "NameValidate";

    [Header("Settings")]
    [SerializeField] private int minNameLength = 2;
    [SerializeField] private int maxNameLength = 12;

    /// <summary>Fired when a valid name has been confirmed.</summary>
    public static event Action<string> OnNameValidated;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        Debug.Log("[NameInputController] Awake called.");

        if (validateButton != null)
        {
            validateButton.onClick.AddListener(OnValidatePressed);
            Debug.Log("[NameInputController] Listener added to validateButton.");
        }
        else
        {
            Debug.LogError("[NameInputController] validateButton is NULL — assign BTN_ValideName in the Inspector.");
        }

        if (nameInputField != null)
        {
            nameInputField.characterLimit = maxNameLength;
            nameInputField.onValueChanged.AddListener(OnInputChanged);
            nameInputField.onSelect.AddListener(OnFieldSelected);
            nameInputField.onSubmit.AddListener(OnSubmit);
        }
        else
        {
            Debug.LogError("[NameInputController] nameInputField is NULL — assign InputFieldName in the Inspector.");
        }

        if (targetAnimator == null)
            Debug.LogError("[NameInputController] targetAnimator is NULL — assign the ZombieTutoManager Animator in the Inspector.");

        HideError();
    }

    private void OnDestroy()
    {
        if (validateButton != null)
            validateButton.onClick.RemoveListener(OnValidatePressed);

        if (nameInputField != null)
        {
            nameInputField.onValueChanged.RemoveListener(OnInputChanged);
            nameInputField.onSelect.RemoveListener(OnFieldSelected);
            nameInputField.onSubmit.RemoveListener(OnSubmit);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Makes the panel visible and focuses the input field.</summary>
    public void Show()
    {
        if (panel != null) panel.SetActive(true);

        if (nameInputField != null)
        {
            nameInputField.text = string.Empty;
            nameInputField.ActivateInputField();
        }

        ShowNormalState();
        SetNameOk(false);
    }

    /// <summary>Hides the panel without validating.</summary>
    public void Hide() => panel?.SetActive(false);

    // ── Input events ──────────────────────────────────────────────────────────

    private void OnFieldSelected(string _) => ShowNormalState();

    private void OnInputChanged(string _)
    {
        ShowNormalState();
        SetNameOk(false);
    }

    private void OnSubmit(string _) => OnValidatePressed();

    // ── Validation ────────────────────────────────────────────────────────────

    private void OnValidatePressed()
    {
        Debug.Log("[NameInputController] OnValidatePressed called.");

        string playerName = nameInputField != null
            ? nameInputField.text.Trim()
            : string.Empty;

        Debug.Log($"[NameInputController] Name entered : '{playerName}' (length {playerName.Length})");

        if (!IsValid(playerName, out string errorMessage))
        {
            Debug.LogWarning($"[NameInputController] Invalid name : {errorMessage}");
            ShowErrorState(errorMessage);
            SetNameOk(false);
            return;
        }

        Debug.Log("[NameInputController] Name valid — setting NameOk=true and firing trigger.");
        SetNameOk(true);
        FireValidateTrigger();

        RegisterPlayer(playerName);
        OnNameValidated?.Invoke(playerName);
    }

    private bool IsValid(string name, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < minNameLength)
        {
            errorMessage = $"Le nom doit contenir au moins {minNameLength} caractères.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private void RegisterPlayer(string playerName)
    {
        SaveSystem.Instance?.SetPlayerName(playerName);
        Debug.Log($"[NameInputController] Nom validé : '{playerName}'");
    }

    // ── Animator ──────────────────────────────────────────────────────────────

    private void SetNameOk(bool value)
    {
        if (targetAnimator != null && !string.IsNullOrEmpty(nameOkParameter))
            targetAnimator.SetBool(nameOkParameter, value);
    }

    private void FireValidateTrigger()
    {
        if (targetAnimator != null && !string.IsNullOrEmpty(nameValidateTrigger))
        {
            targetAnimator.SetTrigger(nameValidateTrigger);
            Debug.Log($"[NameInputController] Trigger '{nameValidateTrigger}' fired on '{targetAnimator.gameObject.name}'.");
        }
        else
        {
            Debug.LogError($"[NameInputController] Cannot fire trigger — animator={targetAnimator}, trigger='{nameValidateTrigger}'");
        }
    }

    // ── Display states ────────────────────────────────────────────────────────

    /// <summary>Normal state: placeholder + name text visible, error hidden.</summary>
    private void ShowNormalState()
    {
        bool hasText = nameInputField != null && nameInputField.text.Length > 0;

        SetVisible(placeholderText, !hasText);
        SetVisible(nameText,        true);
        SetVisible(errorText,       false);
    }

    /// <summary>Error state: only error text visible.</summary>
    private void ShowErrorState(string message)
    {
        SetVisible(placeholderText, false);
        SetVisible(nameText,        false);

        if (errorText != null)
        {
            errorText.text = message;
            SetVisible(errorText, true);
        }
    }

    private void HideError() => ShowNormalState();

    private static void SetVisible(TMP_Text target, bool visible)
    {
        if (target != null) target.gameObject.SetActive(visible);
    }
}
