using System.Collections.Generic;
using UnityEngine;

public class GlobuleRegistry : MonoBehaviour
{
    public static GlobuleRegistry Instance { get; private set; }

    private readonly HashSet<Vector2Int> _positions = new HashSet<Vector2Int>();

    private void Awake() => Instance = this;

    /// <summary>Enregistre la position d'un globule.</summary>
    public void Register(Vector2Int cell) => _positions.Add(cell);

    /// <summary>Supprime la position d'un globule.</summary>
    public void Unregister(Vector2Int cell) => _positions.Remove(cell);

    /// <summary>Retourne true si un globule occupe cette cellule.</summary>
    public bool IsOccupied(Vector2Int cell) => _positions.Contains(cell);

    /// <summary>Retourne true si la ligne contient au moins un globule.</summary>
    public bool RowHasAnyGlobule(int row, int numColumns)
    {
        for (int col = 0; col < numColumns; col++)
            if (_positions.Contains(new Vector2Int(col, row))) return true;
        return false;
    }
}
