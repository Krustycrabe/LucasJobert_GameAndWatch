using UnityEngine;

/// <summary>
/// Placed on a child GameObject of each obstacle with a larger trigger CircleCollider2D.
/// When a virus (tag "Player") enters, tells SlowMotionManager proximity = true.
/// When all viruses leave, tells SlowMotionManager proximity = false.
///
/// SlowMotionManager then evaluates whether the virus is split before activating slow-mo.
///
/// Setup: ObstacleMover.Start() creates this child automatically — no manual prefab
/// editing required.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class ObstacleNearMissTrigger : MonoBehaviour
{
    private int _playersInside;

    private void Awake()
    {
        var col      = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
    }

    private void OnDisable()
    {
        // If the obstacle is destroyed while a virus is inside, release the lock.
        if (_playersInside > 0)
        {
            _playersInside = 0;
            SlowMotionManager.Instance?.SetProximity(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _playersInside++;
        SlowMotionManager.Instance?.SetProximity(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _playersInside = Mathf.Max(0, _playersInside - 1);
        if (_playersInside == 0)
            SlowMotionManager.Instance?.SetProximity(false);
    }
}
