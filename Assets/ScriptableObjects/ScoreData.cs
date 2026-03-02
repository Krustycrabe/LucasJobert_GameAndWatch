using UnityEngine;

[CreateAssetMenu(fileName = "ScoreData", menuName = "GameAndWatch/Score Data")]
public class ScoreData : ScriptableObject
{
    [Header("Configuration")]
    [Tooltip("Points gagnés par atteinte du cœur (avant multiplicateur).")]
    public int basePointsPerHeart = 100;

    [Tooltip("Valeur du multiplicateur ajouté à chaque atteinte consécutive.")]
    public int multiplierIncrement = 1;

    [Header("Runtime — lecture seule")]
    public int currentScore;
    public int currentMultiplier;

    /// <summary>Remet les valeurs runtime à zéro.</summary>
    public void Reset()
    {
        currentScore = 0;
        currentMultiplier = 1;
    }
}
