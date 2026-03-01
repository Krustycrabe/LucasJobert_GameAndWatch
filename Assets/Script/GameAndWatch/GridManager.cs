using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int columns = 10;
    [SerializeField] private int rows = 6;
    [SerializeField] private float cellSize = 1.6f;
    [Tooltip("Coin bas-gauche de la grille en world space.")]
    [SerializeField] private Vector2 gridOrigin = new Vector2(-8f, -4.8f);

    public static GridManager Instance { get; private set; }

    private bool[,] _occupied;

    public int Columns => columns;
    public int Rows => rows;
    public float CellSize => cellSize;

    private void Awake()
    {
        Instance = this;
        _occupied = new bool[columns, rows];
    }

    /// <summary>Convertit une cellule en position world (centre de la cellule).</summary>
    public Vector2 CellToWorld(Vector2Int cell) =>
        gridOrigin + new Vector2(cell.x * cellSize + cellSize * 0.5f,
                                  cell.y * cellSize + cellSize * 0.5f);

    /// <summary>Convertit une position world en cellule.</summary>
    public Vector2Int WorldToCell(Vector2 world) =>
        new Vector2Int(Mathf.FloorToInt((world.x - gridOrigin.x) / cellSize),
                       Mathf.FloorToInt((world.y - gridOrigin.y) / cellSize));

    public bool IsInBounds(Vector2Int cell) =>
        cell.x >= 0 && cell.x < columns && cell.y >= 0 && cell.y < rows;

    public bool IsOccupied(Vector2Int cell) =>
        IsInBounds(cell) && _occupied[cell.x, cell.y];

    public void SetOccupied(Vector2Int cell, bool value)
    {
        if (IsInBounds(cell)) _occupied[cell.x, cell.y] = value;
    }

    #if UNITY_EDITOR
private void OnDrawGizmos()
{
    UnityEditor.Handles.color = new Color(0f, 1f, 0.5f, 0.4f);

    for (int x = 0; x < columns; x++)
    {
        for (int y = 0; y < rows; y++)
        {
            Vector2 center = gridOrigin + new Vector2(x * cellSize + cellSize * 0.5f,
                                                       y * cellSize + cellSize * 0.5f);
            UnityEditor.Handles.DrawWireCube(center, new Vector3(cellSize, cellSize, 0f));
        }
    }

    // Affiche les coordonnées de chaque cellule
    UnityEditor.Handles.color = Color.white;
    for (int x = 0; x < columns; x++)
    {
        for (int y = 0; y < rows; y++)
        {
            Vector2 center = gridOrigin + new Vector2(x * cellSize + cellSize * 0.5f,
                                                       y * cellSize + cellSize * 0.5f);
            UnityEditor.Handles.Label(center, $"{x},{y}");
        }
    }
}
#endif

}
