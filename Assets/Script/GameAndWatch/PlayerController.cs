using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player movement on the grid via directional swipe (new InputSystem).
///
/// INPUT ASSET: Assets/Inputs/GameAndWatchActions.inputactions
///   Action Map  : GameAndWatch
///   TouchContact: Button — fires when finger/mouse presses down
///   TouchPosition: Value/Vector2 — current pointer position
///
/// SWIPE MODEL:
///   On contact start: record _touchStart.
///   Each frame: compare current position against _touchStart.
///   When delta >= swipeThreshold on the dominant axis → move 1 cell.
///   Input is then locked (_moving = true) until the player reaches the target
///   cell (smooth lerp). Only one move per gesture.
///
/// RESPAWN:
///   Priority: free cell with 2 free cells ahead → any free cell → startCell.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private Vector2Int startCell   = new Vector2Int(0, 3);
    [SerializeField] private int        heartColumn = 7;

    [Header("Swipe Input")]
    [Tooltip("Minimum screen-space distance (pixels) to count as a swipe.")]
    [SerializeField] private float swipeThreshold = 40f;

    [Header("Movement")]
    [Tooltip("Time in seconds to slide from one cell to the next.")]
    [SerializeField] private float moveDuration = 0.08f;

    [Header("Input Asset")]
    [Tooltip("Drag the generated GameAndWatchActions asset here.")]
    [SerializeField] private InputActionAsset inputActionsAsset;

    public static event Action OnPlayerReachedHeart;

    private Vector2Int      _currentCell;
    private GridManager     _grid;
    private GlobuleRegistry _registry;

    // ── Input ─────────────────────────────────────────────────────────────────
    private InputAction _contactAction;
    private InputAction _positionAction;

    private bool    _touching;
    private Vector2 _touchStart;
    private bool    _swipeConsumed; // one move per press contact

    // ── State ─────────────────────────────────────────────────────────────────
    private bool      _inputEnabled = true;
    private bool      _moving;       // true while lerp coroutine runs
    private Coroutine _moveCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (inputActionsAsset != null)
        {
            var map = inputActionsAsset.FindActionMap("GameAndWatch");
            if (map != null)
            {
                _contactAction  = map.FindAction("TouchContact");
                _positionAction = map.FindAction("TouchPosition");
            }
        }

        if (_contactAction == null || _positionAction == null)
        {
            Debug.LogWarning("[PlayerController] Input asset not found — using runtime fallback.");
            _contactAction = new InputAction("TouchContact", InputActionType.Button);
            _contactAction.AddBinding("<Touchscreen>/primaryTouch/press");
            _contactAction.AddBinding("<Mouse>/leftButton");

            _positionAction = new InputAction("TouchPosition", InputActionType.Value);
            _positionAction.expectedControlType = "Vector2";
            _positionAction.AddBinding("<Touchscreen>/primaryTouch/position");
            _positionAction.AddBinding("<Mouse>/position");

            _contactAction.Enable();
            _positionAction.Enable();
        }
        else
        {
            // Enable the whole asset so all maps and actions are active.
            inputActionsAsset.Enable();
            Debug.Log("[PlayerController] Input asset enabled.");
        }
    }

    private void OnDestroy()
    {
        // Only dispose runtime-created actions, not asset-owned ones.
        if (inputActionsAsset == null)
        {
            _contactAction?.Dispose();
            _positionAction?.Dispose();
        }
    }

    private void Start()
    {
        _grid     = GridManager.Instance;
        _registry = GlobuleRegistry.Instance;

        if (_grid == null)
            Debug.LogError("[PlayerController] GridManager.Instance is null — check script execution order.");

        ResetPosition();
    }

    private void OnEnable()
    {
        LivesManager.OnPlayerReset += ResetPosition;
        LivesManager.OnGameOver    += DisableInput;
    }

    private void OnDisable()
    {
        LivesManager.OnPlayerReset -= ResetPosition;
        LivesManager.OnGameOver    -= DisableInput;
    }

    private void Update()
    {
        if (!_inputEnabled || _moving) return;

        // Read press state directly from devices — reliable across Editor, Remote, and builds.
        bool isPressed = (Mouse.current    != null && Mouse.current.leftButton.isPressed) ||
                         (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed);

        // Read position from the InputAction (works for both mouse and touch via asset bindings).
        Vector2 currentPos = _positionAction.ReadValue<Vector2>();

        if (!isPressed)
        {
            if (_touching)
            {
                _touching      = false;
                _swipeConsumed = false;
            }
            return;
        }

        if (!_touching)
        {
            _touching      = true;
            _swipeConsumed = false;
            _touchStart    = currentPos;
            return;
        }

        if (_swipeConsumed) return;

        Vector2 delta = currentPos - _touchStart;
        if (delta.magnitude < swipeThreshold) return;

        Vector2Int dir;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            dir = delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            dir = delta.y > 0 ? Vector2Int.up : Vector2Int.down;

        _swipeConsumed = true;
        TryMove(dir);
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    private void TryMove(Vector2Int direction)
    {
        if (_grid == null || _moving) return;

        Vector2Int target = _currentCell + direction;
        if (!_grid.IsInBounds(target)) return;

        _currentCell = target;

        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(SlideTo(_grid.CellToWorld(_currentCell)));

        if (_currentCell.x == heartColumn)
            OnPlayerReachedHeart?.Invoke();
    }

    /// <summary>Smoothly slides the player parent to the target world position.</summary>
    private IEnumerator SlideTo(Vector2 targetWorld)
    {
        _moving = true;

        Vector3 from    = transform.parent.position;
        Vector3 to      = new Vector3(targetWorld.x, targetWorld.y, from.z);
        float   elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            transform.parent.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / moveDuration));
            yield return null;
        }

        transform.parent.position = to;
        _moving        = false;
        _moveCoroutine = null;
    }

    // ── Respawn ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Resets the player to a safe cell in the starting column.
    /// Priority: free cell with 2 free cells ahead → any free cell → startCell.
    /// </summary>
    public void ResetPosition()
    {
        if (_grid == null) return;

        // Cancel any in-progress slide.
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }
        _moving       = false;
        _inputEnabled = true;
        _touching     = false;
        _swipeConsumed = false;

        _currentCell = FindSafeRespawnCell();
        transform.parent.position = _grid.CellToWorld(_currentCell);
    }

    private Vector2Int FindSafeRespawnCell()
    {
        if (_registry == null) return startCell;

        int col  = startCell.x;
        int rows = _grid.Rows;

        var preferred = new List<Vector2Int>();
        for (int row = 0; row < rows; row++)
        {
            var cell = new Vector2Int(col, row);
            if (!_registry.IsOccupied(cell) && HasFreeAhead(cell, 2))
                preferred.Add(cell);
        }
        if (preferred.Count > 0)
            return preferred[UnityEngine.Random.Range(0, preferred.Count)];

        var fallback = new List<Vector2Int>();
        for (int row = 0; row < rows; row++)
        {
            var cell = new Vector2Int(col, row);
            if (!_registry.IsOccupied(cell))
                fallback.Add(cell);
        }
        if (fallback.Count > 0)
            return fallback[UnityEngine.Random.Range(0, fallback.Count)];

        return startCell;
    }

    /// <summary>Returns true if the next <paramref name="ahead"/> cells to the right are all globule-free.</summary>
    private bool HasFreeAhead(Vector2Int origin, int ahead)
    {
        for (int i = 1; i <= ahead; i++)
        {
            var cell = new Vector2Int(origin.x + i, origin.y);
            if (!_grid.IsInBounds(cell) || _registry.IsOccupied(cell))
                return false;
        }
        return true;
    }

    private void DisableInput()
    {
        _inputEnabled = false;
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
            _moving = false;
        }
    }
}
