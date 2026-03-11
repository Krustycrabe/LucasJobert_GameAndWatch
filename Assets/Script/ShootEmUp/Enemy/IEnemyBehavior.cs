/// <summary>
/// Contract for all enemy behavior scripts. EnemyCore drives the lifecycle via this interface.
/// </summary>
public interface IEnemyBehavior
{
    /// <summary>Called once by EnemyCore after Awake. Use to cache references.</summary>
    void Initialize(EnemyCore core);

    /// <summary>Called every frame by EnemyCore while the enemy is alive.</summary>
    void OnUpdate();

    /// <summary>Called by EnemyCore when health reaches zero.</summary>
    void OnDeath();
}
