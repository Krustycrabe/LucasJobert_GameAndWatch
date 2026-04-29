using UnityEngine;

namespace GameAndWatch.Audio
{
    /// <summary>
    /// Starts a music track when the scene loads.
    /// Add this component to any persistent GameObject in the scene.
    /// </summary>
    public class SceneMusicStarter : MonoBehaviour
    {
        [Tooltip("Sound ID of the music to play. Must match a SoundConfig with SoundType = Music in the SoundLibrary.")]
        [SerializeField] private string musicId;

        private void Start()
        {
            if (!string.IsNullOrEmpty(musicId))
                AudioManager.Instance?.PlayMusic(musicId);
        }
    }
}
