using UnityEngine;

public class GlobuleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject globulePrefab;

    [Tooltip("Colonne fixe de spawn.")]
    [SerializeField] private int spawnColumn = 8;

    [Tooltip("Probabilité de spawn sur chaque ligne à chaque battement (0 à 1).")]
    [SerializeField][Range(0f, 1f)] private float spawnChancePerRow = 0.6f;

    [Tooltip("Nombre minimum de cellules libres à garantir par ligne (passage du joueur).")]
    [SerializeField] private int minFreeCellsPerRow = 2;

    private void OnEnable() => HeartAnimator.OnHeartBeat1 += SpawnOnAllEligibleRows;
    private void OnDisable() => HeartAnimator.OnHeartBeat1 -= SpawnOnAllEligibleRows;

    private void SpawnOnAllEligibleRows()
    {
        GridManager grid = GridManager.Instance;
        GlobuleRegistry registry = GlobuleRegistry.Instance;

        for (int row = 0; row < grid.Rows; row++)
        {
            Vector2Int spawnCell = new Vector2Int(spawnColumn, row);

            if (registry.IsOccupied(spawnCell)) continue;

            // Vérifie toute la ligne (pas seulement depuis spawnColumn)
            int freeCells = registry.CountFreeCellsInRow(row, 0, grid.Columns - 1);
            if (freeCells <= minFreeCellsPerRow) continue;

            if (Random.value > spawnChancePerRow) continue;

            SpawnAt(grid, spawnCell);
        }
    }

    private void SpawnAt(GridManager grid, Vector2Int cell)
    {
        GameObject globule = Instantiate(globulePrefab, grid.CellToWorld(cell), Quaternion.identity);

        if (globule.TryGetComponent(out GlobuleController controller))
            controller.Initialize(cell);
    }
}
