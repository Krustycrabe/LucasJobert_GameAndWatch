using System.Collections.Generic;
using UnityEngine;

public class GlobuleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject globulePrefab;

    [Tooltip("Colonne fixe de spawn.")]
    [SerializeField] private int spawnColumn = 7;

    [Tooltip("Probabilité de spawn sur chaque ligne à chaque battement (0 à 1).")]
    [SerializeField][Range(0f, 1f)] private float spawnChancePerRow = 0.6f;

    [Tooltip("Nombre minimum de lignes entièrement libres à garantir à tout moment.")]
    [SerializeField] private int minFreeRows = 1;

    private void Awake()
    {
        DifficultyData data = DifficultyManager.Instance?.Current;
        if (data != null) spawnChancePerRow = data.spawnChance; // ← renomme spawnChancePerRow en spawnChance
    }

    private void OnEnable() => HeartAnimator.OnHeartBeat1 += SpawnOnAllEligibleRows;
    private void OnDisable() => HeartAnimator.OnHeartBeat1 -= SpawnOnAllEligibleRows;

    private void SpawnOnAllEligibleRows()
    {
        GridManager grid = GridManager.Instance;
        GlobuleRegistry registry = GlobuleRegistry.Instance;

        // Compte les lignes actuellement libres
        int freeRowCount = 0;
        bool[] rowIsFree = new bool[grid.Rows];
        for (int row = 0; row < grid.Rows; row++)
        {
            rowIsFree[row] = !registry.RowHasAnyGlobule(row, grid.Columns);
            if (rowIsFree[row]) freeRowCount++;
        }

        for (int row = 0; row < grid.Rows; row++)
        {
            Vector2Int spawnCell = new Vector2Int(spawnColumn, row);

            if (registry.IsOccupied(spawnCell)) continue;

            // Si la ligne est libre, vérifier qu'on peut se permettre de l'occuper
            if (rowIsFree[row])
            {
                if (freeRowCount <= minFreeRows) continue;
                freeRowCount--; // On va l'occuper, on met à jour le compteur
            }

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
