using System.Collections;
using UnityEngine;
using GameAndWatch.Audio;

/// <summary>
/// Instantiates bullets at the spawn point at a configurable fire rate.
/// </summary>
public class BulletSpawner : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float fireRate = 0.15f;

    private Coroutine _shootingCoroutine;

    /// <summary>Begins spawning bullets.</summary>
    public void StartShooting()
    {
        if (_shootingCoroutine != null) return;
        _shootingCoroutine = StartCoroutine(ShootingRoutine());
    }

    /// <summary>Stops bullet spawning.</summary>
    public void StopShooting()
    {
        if (_shootingCoroutine == null) return;
        StopCoroutine(_shootingCoroutine);
        _shootingCoroutine = null;
    }

    private IEnumerator ShootingRoutine()
    {
        var interval = new WaitForSeconds(fireRate);
        while (true)
        {
            Instantiate(bulletPrefab, spawnPoint.position, spawnPoint.rotation);
            AudioManager.Instance?.PlayOneShot(SoundIds.ShootEmUp.PlayerShoot);
            yield return interval;
        }
    }
}
