using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads touch input and broadcasts C# events. Single point of contact with the Input System.
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    public event Action<Vector2> OnTouchBegan;
    public event Action<Vector2> OnTouchMoved;
    public event Action OnTouchEnded;

    private ShootEmUpInputActions _actions;
    private bool _isTouching;

    private void Awake()
    {
        _actions = new ShootEmUpInputActions();
    }

    private void OnEnable()
    {
        _actions.Enable();
        _actions.ShootEmUp.TouchContact.started += HandleContactStarted;
        _actions.ShootEmUp.TouchContact.canceled += HandleContactCanceled;
        _actions.ShootEmUp.TouchPosition.performed += HandlePositionChanged;
    }

    private void OnDisable()
    {
        _actions.ShootEmUp.TouchContact.started -= HandleContactStarted;
        _actions.ShootEmUp.TouchContact.canceled -= HandleContactCanceled;
        _actions.ShootEmUp.TouchPosition.performed -= HandlePositionChanged;
        _actions.Disable();
    }

    private void HandleContactStarted(InputAction.CallbackContext context)
    {
        _isTouching = true;
        OnTouchBegan?.Invoke(_actions.ShootEmUp.TouchPosition.ReadValue<Vector2>());
    }

    private void HandlePositionChanged(InputAction.CallbackContext context)
    {
        if (!_isTouching) return;
        OnTouchMoved?.Invoke(context.ReadValue<Vector2>());
    }

    private void HandleContactCanceled(InputAction.CallbackContext context)
    {
        _isTouching = false;
        OnTouchEnded?.Invoke();
    }
}
