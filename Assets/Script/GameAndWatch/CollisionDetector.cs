using System;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    public static event Action OnPlayerHit;

    private GridManager _grid;
    private GlobuleRegistry _registry;

    private void Awake()
    {
        _grid = GridManager.Instance;
        _registry = GlobuleRegistry.Instance;
    }

    private void OnEnable() => GameTicker.OnTick += CheckCollision;
    private void OnDisable() => GameTicker.OnTick -= CheckCollision;

    private void CheckCollision()
    {
        Vector2Int playerCell = _grid.WorldToCell(transform.position);
        if (_registry.IsOccupied(playerCell))
            OnPlayerHit?.Invoke();
    }
}
