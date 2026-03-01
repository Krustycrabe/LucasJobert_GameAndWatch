using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Tooltip("Cellule de départ du joueur.")]
    [SerializeField] private Vector2Int startCell = new Vector2Int(0, 2);
    [Tooltip("Cellule du cœur à atteindre.")]
    [SerializeField] private Vector2Int heartCell = new Vector2Int(0, 2);

    public static event Action OnPlayerReachedHeart;

    private Vector2Int _currentCell;
    private GridManager _grid;

    private void Awake() => _grid = GridManager.Instance;

    private void OnEnable() => LivesManager.OnPlayerReset += ResetPosition;
    private void OnDisable() => LivesManager.OnPlayerReset -= ResetPosition;

    private void Start() => ResetPosition();

    /// <summary>Branché sur le PlayerInput component via l'Input System.</summary>
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        Vector2 raw = ctx.ReadValue<Vector2>();
        Vector2Int direction = Vector2Int.RoundToInt(raw);
        TryMove(direction);
    }

    private void TryMove(Vector2Int direction)
    {
        Vector2Int target = _currentCell + direction;
        if (!_grid.IsInBounds(target)) return;

        _currentCell = target;
        transform.position = _grid.CellToWorld(_currentCell);

        if (_currentCell == heartCell)
            OnPlayerReachedHeart?.Invoke();
    }

    public void ResetPosition()
    {
        _currentCell = startCell;
        transform.position = _grid.CellToWorld(startCell);
    }
}
