using UnityEngine;

/// <summary>
/// Holds all static data for an enemy type. Assign one instance per enemy variant.
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "ShootEmUp/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    [Header("Health")]
    public int maxHealth = 3;

    [Header("Movement")]
    public float moveSpeed = 2f;

    [Header("Rewards")]
    public int scoreValue = 100;
    public int energyDropAmount = 5;
    public GameObject energyCellPrefab;

    [Header("Combat")]
    public float shootRate = 1.5f;
}
