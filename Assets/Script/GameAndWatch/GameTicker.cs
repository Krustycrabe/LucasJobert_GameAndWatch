using System;
using System.Collections;
using UnityEngine;

public class GameTicker : MonoBehaviour
{
    [SerializeField] private float tickInterval = 1f;

    public static event Action OnTick;

    private Coroutine _tickCoroutine;

    private void Start()
    {
        DifficultyData data = DifficultyManager.Instance?.Current;
        if (data != null) tickInterval = data.tickInterval;
        _tickCoroutine = StartCoroutine(TickRoutine());
    }


    /// <summary>Change l'intervalle de tick à chaud (gestion de la difficulté).</summary>
    public void SetTickInterval(float interval)
    {
        tickInterval = Mathf.Max(0.1f, interval);

        if (_tickCoroutine != null) StopCoroutine(_tickCoroutine);
        _tickCoroutine = StartCoroutine(TickRoutine());
    }

    private IEnumerator TickRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(tickInterval);
            OnTick?.Invoke();
        }
    }
}
