using UnityEngine;

public class GlobuleController : MonoBehaviour
{
    [Tooltip("1 = droite, -1 = gauche.")]
    [SerializeField] private int moveDirectionX = -1;

    private Vector2Int _currentCell;
    private GridManager _grid;
    private GlobuleRegistry _registry;

    private void OnEnable() => GameTicker.OnTick += Move;
    private void OnDisable() => GameTicker.OnTick -= Move;

    /// <summary>Appelé par GlobuleSpawner juste après Instantiate.</summary>
    public void Initialize(Vector2Int startCell)
    {
        _grid = GridManager.Instance;
        _registry = GlobuleRegistry.Instance;

        _currentCell = startCell;
        _registry.Register(_currentCell);
        transform.position = _grid.CellToWorld(_currentCell);
    }

    private void Move()
    {
        if (_grid == null || _registry == null)
        {
            Debug.LogError("[GlobuleController] GridManager ou GlobuleRegistry introuvable.");
            return;
        }

        Vector2Int nextCell = new Vector2Int(_currentCell.x + moveDirectionX, _currentCell.y);

        _registry.Unregister(_currentCell);

        if (!_grid.IsInBounds(nextCell))
        {
            Destroy(gameObject);
            return;
        }

        _currentCell = nextCell;
        _registry.Register(_currentCell);
        transform.position = _grid.CellToWorld(_currentCell);
    }
}
