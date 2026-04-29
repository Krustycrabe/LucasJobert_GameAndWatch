using UnityEngine;

namespace GameAndWatch.Audio
{
    /// <summary>
    /// Lightweight bridge to be added on any animated GameObject.
    /// Forwards Animation Event calls to the AudioManager singleton.
    /// </summary>
    public class AnimationAudioBridge : MonoBehaviour
    {
        /// <summary>Animation Event: plays a one-shot sound by ID.</summary>
        public void PlayOneShot(string soundId) => AudioManager.Instance.PlayOneShot(soundId);

        /// <summary>Animation Event: starts a looping sound by ID.</summary>
        public void PlayLoop(string soundId) => AudioManager.Instance.PlayLoop(soundId);

        /// <summary>Animation Event: stops a looping sound by ID.</summary>
        public void StopLoop(string soundId) => AudioManager.Instance.StopLoop(soundId);

        /// <summary>Animation Event: plays music by ID with crossfade.</summary>
        public void PlayMusic(string soundId) => AudioManager.Instance.PlayMusic(soundId);

        /// <summary>Animation Event: stops the current music with fade-out.</summary>
        public void StopMusic(string soundId) => AudioManager.Instance.StopMusic();
    }
}
