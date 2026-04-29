using UnityEngine;
using UnityEngine.Audio;

namespace GameAndWatch.Audio
{
    public enum SoundType
    {
        OneShot,
        Loop,
        Music
    }

    [CreateAssetMenu(fileName = "SoundConfig", menuName = "Audio/Sound Config")]
    public class SoundConfig : ScriptableObject
    {
        [Header("Identity")]
        public string soundId;
        public SoundType soundType;

        [Header("Clips")]
        [Tooltip("Multiple clips: one will be picked at random on each play.")]
        public AudioClip[] clips;

        [Header("Mixer")]
        public AudioMixerGroup mixerGroup;

        [Header("Volume")]
        [Range(0f, 1f)] public float volume = 1f;

        [Header("Pitch")]
        [Range(-3f, 3f)] public float basePitch = 1f;
        public bool randomPitch = false;
        [Range(0f, 1f)] public float pitchVariance = 0.1f;

        [Header("Cooldown")]
        [Tooltip("Minimum time in seconds before this sound can play again. Useful to avoid stacking when many sources trigger it simultaneously. 0 = no limit.")]
        public float minInterval = 0f;

        [Header("Playback")]
        [Tooltip("Delay in seconds before the sound actually plays after being triggered. Applies to loops only.")]
        public float startDelay = 0f;

        [Header("Low-Pass Filter")]
        public bool useLowPassFilter = false;
        [Range(10f, 22000f)] public float lowPassCutoff = 5000f;
        [Range(1f, 10f)] public float lowPassResonance = 1f;

        [Header("Loop Settings")]
        [Tooltip("Used only when SoundType is Loop.")]
        public bool loopOnStart = false;

        /// <summary>Returns a random clip from the clips array, or null if empty.</summary>
        public AudioClip GetClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        /// <summary>Returns the computed pitch, with optional random variance applied.</summary>
        public float GetPitch()
        {
            if (!randomPitch) return basePitch;
            return basePitch + Random.Range(-pitchVariance, pitchVariance);
        }
    }
}
