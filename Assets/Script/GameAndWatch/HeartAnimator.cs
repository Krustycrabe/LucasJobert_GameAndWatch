using System;
using UnityEngine;

public class HeartAnimator : MonoBehaviour
{
    [SerializeField] private GameObject heartBeat1;
    [SerializeField] private GameObject heartBeat2;
    [SerializeField] private GameObject heartBeat3;

    [Tooltip("Séquence de battements (0=Beat1, 1=Beat2, 2=Beat3).")]
    [SerializeField] private int[] beatSequence = { 0, 1, 2, 1 };

    public static event Action OnHeartBeat1;

    private GameObject[] _beats;
    private int _sequenceIndex = 0;

    private void Awake()
    {
        _beats = new[] { heartBeat1, heartBeat2, heartBeat3 };
        ShowBeat(beatSequence[0]);
    }

    private void OnEnable() => GameTicker.OnTick += AdvanceBeat;
    private void OnDisable() => GameTicker.OnTick -= AdvanceBeat;

    private void AdvanceBeat()
    {
        _sequenceIndex = (_sequenceIndex + 1) % beatSequence.Length;
        int beatIndex = beatSequence[_sequenceIndex];
        ShowBeat(beatIndex);

        if (beatIndex == 0)
            OnHeartBeat1?.Invoke();
    }

    private void ShowBeat(int index)
    {
        for (int i = 0; i < _beats.Length; i++)
            _beats[i].SetActive(i == index);
    }
}
