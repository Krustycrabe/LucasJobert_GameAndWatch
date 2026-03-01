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

    /// <summary>Compte les cellules libres dans une ligne entre deux colonnes.</summary>
    public int CountFreeCellsInRow(int row, int fromCol, int toCol)
    {
        int free = 0;
        for (int col = fromCol; col <= toCol; col++)
            if (!_positions.Contains(new Vector2Int(col, row))) free++;
        return free;
    }
}
