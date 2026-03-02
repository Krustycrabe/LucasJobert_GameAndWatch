using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyData", menuName = "GameAndWatch/Difficulty Data")]
public class DifficultyData : ScriptableObject
{
    [Header("Identification")]
    public string difficultyName = "Normal";

    [Header("Paramètres")]
    [Tooltip("Nombre de cœurs au départ.")]
    [Min(1)] public int startingLives = 3;

    [Tooltip("Intervalle entre chaque tick en secondes. Plus bas = plus rapide.")]
    [Min(0.1f)] public float tickInterval = 1f;

    [Tooltip("Probabilité de spawn d'un globule par ligne à chaque battement (0 à 1).")]
    [Range(0f, 1f)] public float spawnChance = 0.4f;
}
