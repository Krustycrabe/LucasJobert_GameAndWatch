using System.Collections.Generic;
using UnityEngine;

namespace GameAndWatch.Audio
{
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [SerializeField] private List<SoundConfig> sounds = new();

        private Dictionary<string, SoundConfig> _lookup;

        /// <summary>Builds the internal lookup dictionary. Called automatically by AudioManager on init.</summary>
        public void Initialize()
        {
            _lookup = new Dictionary<string, SoundConfig>(sounds.Count);
            foreach (SoundConfig config in sounds)
            {
                if (config == null) continue;
                if (!_lookup.TryAdd(config.soundId, config))
                    Debug.LogWarning($"[SoundLibrary] Duplicate sound ID: '{config.soundId}'. Only the first entry will be used.");
            }
        }

        /// <summary>Returns the SoundConfig for the given ID, or null if not found.</summary>
        public SoundConfig Get(string soundId)
        {
            if (_lookup == null) Initialize();
            _lookup.TryGetValue(soundId, out SoundConfig config);
            if (config == null)
                Debug.LogWarning($"[SoundLibrary] Sound ID not found: '{soundId}'.");
            return config;
        }
    }
}
