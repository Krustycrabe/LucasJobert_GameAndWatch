using System;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    [Tooltip("Durée d'invincibilité après un coup (secondes).")]
    [SerializeField] private float invincibilityDuration = 2f;

    public static event Action OnPlayerHit;

    private GridManager _grid;
    private GlobuleRegistry _registry;
    private float _invincibilityTimer;
    private bool _gameOver;

    private void Start()
    {
        _grid = GridManager.Instance;
        _registry = GlobuleRegistry.Instance;
    }

    private void OnEnable()
    {
        LivesManager.OnPlayerReset += StartInvincibility;
        LivesManager.OnGameOver += StopDetection;
    }

    private void OnDisable()
    {
        LivesManager.OnPlayerReset -= StartInvincibility;
        LivesManager.OnGameOver -= StopDetection;
    }

    private void Update()
    {
        if (_gameOver || _grid == null || _registry == null) return;

        if (_invincibilityTimer > 0f)
        {
            _invincibilityTimer -= Time.deltaTime;
            return;
        }

        Vector2Int playerCell = _grid.WorldToCell(transform.position);
        if (_registry.IsOccupied(playerCell))
        {
            Debug.Log($"[CollisionDetector] Hit à la cellule {playerCell}");
            _invincibilityTimer = invincibilityDuration;
            OnPlayerHit?.Invoke();
        }
    }

    private void StartInvincibility()
    {
        _invincibilityTimer = invincibilityDuration;
        Debug.Log("[CollisionDetector] Invincibilité démarrée");
    }

    private void StopDetection() => _gameOver = true;
}
