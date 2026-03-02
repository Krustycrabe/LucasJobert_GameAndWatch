using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Vector2Int startCell = new Vector2Int(0, 3);
    [SerializeField] private int heartColumn = 7;

    public static event Action OnPlayerReachedHeart;

    private Vector2Int _currentCell;
    private GridManager _grid;

    private void Start()
    {
        _grid = GridManager.Instance;
        ResetPosition();
    }

    private void OnEnable() => LivesManager.OnPlayerReset += ResetPosition;
    private void OnDisable() => LivesManager.OnPlayerReset -= ResetPosition;

    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        TryMove(Vector2Int.RoundToInt(ctx.ReadValue<Vector2>()));
    }

    /// <summary>Déplace le joueur vers le haut.</summary>
    public void MoveUp() => TryMove(Vector2Int.up);

    /// <summary>Déplace le joueur vers le bas.</summary>
    public void MoveDown() => TryMove(Vector2Int.down);

    /// <summary>Déplace le joueur vers la gauche.</summary>
    public void MoveLeft() => TryMove(Vector2Int.left);

    /// <summary>Déplace le joueur vers la droite.</summary>
    public void MoveRight() => TryMove(Vector2Int.right);

    /// <summary>Remet le joueur à sa position de départ.</summary>
    public void ResetPosition()
    {
        if (_grid == null) return;
        _currentCell = startCell;
        transform.parent.position = _grid.CellToWorld(startCell);
    }

    private void TryMove(Vector2Int direction)
    { 
        if (_grid == null) return;

        Vector2Int target = _currentCell + direction;
        if (!_grid.IsInBounds(target)) return;

        _currentCell = target;
        transform.parent.position = _grid.CellToWorld(_currentCell);

        if (_currentCell.x == heartColumn)
            OnPlayerReachedHeart?.Invoke();
    }
}
