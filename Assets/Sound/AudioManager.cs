using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GameAndWatch.Audio
{
    /// <summary>
    /// Central audio manager. Singleton — persist across scenes.
    /// Supports one-shot SFX, looping sounds, and music with crossfade.
    /// Callable from code or via Animation Events using the soundId string.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        public static AudioManager Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Library")]
        [SerializeField] private SoundLibrary library;

        [Header("Mixer")]
        [SerializeField] private AudioMixer mainMixer;

        [Header("Music Crossfade")]
        [SerializeField] private float musicFadeDuration = 1f;

        [Header("Low-Pass Filter Transition")]
        [SerializeField] private float lowPassTransitionDuration = 0.5f;

        // ── Private state ──────────────────────────────────────────────────────

        // Pool of reusable AudioSources for one-shots and loops
        private readonly List<AudioSource> _pool = new();

        // Active looping sources keyed by soundId
        private readonly Dictionary<string, AudioSource> _activeLoops = new();

        // Pending loop start coroutines keyed by soundId (while startDelay is running)
        private readonly Dictionary<string, Coroutine> _pendingLoops = new();

        // Cooldown tracking: last play time per soundId, for sounds with minInterval set
        private readonly Dictionary<string, float> _lastPlayTime = new();

        // Dedicated music sources for crossfading
        private AudioSource _musicSourceA;
        private AudioSource _musicSourceB;
        private bool        _musicOnA;       // true = A is currently active
        private string      _currentMusicId; // ID of the active track, null if none

        private Coroutine _musicFadeCoroutine;
        private Coroutine _lowPassCoroutine;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            library.Initialize();
            CreateMusicSources();
        }

        // ── Internal helpers ───────────────────────────────────────────────────

        private void CreateMusicSources()
        {
            _musicSourceA = AddAudioSource();
            _musicSourceB = AddAudioSource();
        }

        private AudioSource AddAudioSource()
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            return src;
        }

        private AudioSource GetPooledSource()
        {
            foreach (AudioSource src in _pool)
            {
                if (!src.isPlaying) return src;
            }

            AudioSource newSrc = AddAudioSource();
            _pool.Add(newSrc);
            return newSrc;
        }

        private void ApplyConfig(AudioSource src, SoundConfig config, AudioClip clip)
        {
            src.clip        = clip;
            src.outputAudioMixerGroup = config.mixerGroup;
            src.volume      = config.volume;
            src.pitch       = config.GetPitch();
            src.loop        = false;

            if (config.useLowPassFilter)
            {
                AudioLowPassFilter lpf = src.GetComponent<AudioLowPassFilter>();
                if (lpf == null) lpf = src.gameObject.AddComponent<AudioLowPassFilter>();
                lpf.cutoffFrequency = config.lowPassCutoff;
                lpf.lowpassResonanceQ = config.lowPassResonance;
                lpf.enabled = true;
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Plays a one-shot sound by ID. Safe to call from Animation Events.</summary>
        public void PlayOneShot(string soundId)
        {
            SoundConfig config = library.Get(soundId);
            if (config == null) return;

            if (config.minInterval > 0f)
            {
                float lastPlay = _lastPlayTime.GetValueOrDefault(soundId, float.NegativeInfinity);
                if (Time.time - lastPlay < config.minInterval) return;
                _lastPlayTime[soundId] = Time.time;
            }

            AudioClip clip = config.GetClip();
            if (clip == null) return;

            AudioSource src = GetPooledSource();
            ApplyConfig(src, config, clip);
            src.loop = false;
            src.Play();
        }

        /// <summary>Starts a looping sound by ID. Does nothing if already playing.</summary>
        public void PlayLoop(string soundId)
        {
            if (_activeLoops.ContainsKey(soundId) || _pendingLoops.ContainsKey(soundId)) return;

            SoundConfig config = library.Get(soundId);
            if (config == null) return;

            if (config.startDelay > 0f)
            {
                Coroutine pending = StartCoroutine(PlayLoopDelayed(soundId, config));
                _pendingLoops[soundId] = pending;
            }
            else
            {
                StartLoopImmediate(soundId, config);
            }
        }

        private void StartLoopImmediate(string soundId, SoundConfig config)
        {
            AudioClip clip = config.GetClip();
            if (clip == null) return;

            AudioSource src = GetPooledSource();
            ApplyConfig(src, config, clip);
            src.loop = true;
            src.Play();
            _activeLoops[soundId] = src;
        }

        private IEnumerator PlayLoopDelayed(string soundId, SoundConfig config)
        {
            yield return new WaitForSeconds(config.startDelay);
            _pendingLoops.Remove(soundId);
            if (!_activeLoops.ContainsKey(soundId))
                StartLoopImmediate(soundId, config);
        }

        /// <summary>Stops a looping sound by ID.</summary>
        public void StopLoop(string soundId)
        {
            // Cancel if still pending (delay not elapsed yet)
            if (_pendingLoops.TryGetValue(soundId, out Coroutine pending))
            {
                StopCoroutine(pending);
                _pendingLoops.Remove(soundId);
                return;
            }

            if (!_activeLoops.TryGetValue(soundId, out AudioSource src)) return;
            src.Stop();
            _activeLoops.Remove(soundId);
        }

        /// <summary>Stops all active loops.</summary>
        public void StopAllLoops()
        {
            foreach (AudioSource src in _activeLoops.Values) src.Stop();
            _activeLoops.Clear();
        }

        /// <summary>
        /// Plays music by ID with a crossfade.
        /// If the same track is already playing, does nothing.
        /// </summary>
        public void PlayMusic(string soundId)
        {
            SoundConfig config = library.Get(soundId);
            if (config == null) return;

            AudioClip clip = config.GetClip();
            if (clip == null) return;

            if (_currentMusicId == soundId) return;
            _currentMusicId = soundId;

            AudioSource current = _musicOnA ? _musicSourceA : _musicSourceB;
            AudioSource next    = _musicOnA ? _musicSourceB : _musicSourceA;

            next.clip   = clip;
            next.outputAudioMixerGroup = config.mixerGroup;
            next.pitch  = config.GetPitch();
            next.loop   = true;
            next.volume = 0f;
            next.Play();

            float targetVolume = config.volume;

            if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine);
            _musicFadeCoroutine = StartCoroutine(CrossfadeMusic(current, next, targetVolume));

            _musicOnA = !_musicOnA;
        }

        /// <summary>Stops the currently playing music with a fade-out.</summary>
        public void StopMusic()
        {
            // After PlayMusic toggles _musicOnA, the active (just-started) source
            // is on the side _musicOnA now points AWAY from.
            AudioSource playing = _musicOnA ? _musicSourceB : _musicSourceA;
            _currentMusicId = null;
            if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine);
            _musicFadeCoroutine = StartCoroutine(FadeOut(playing));
        }

        // ── Low-Pass Filter (Mixer) ────────────────────────────────────────────

        /// <summary>
        /// Applies a low-pass filter effect on the Music mixer group by transitioning
        /// the exposed parameter MusicLowPassCutoff.
        /// </summary>
        /// <param name="cutoff">Target cutoff frequency in Hz (10–22000).</param>
        public void SetMusicLowPass(float cutoff)
        {
            if (_lowPassCoroutine != null) StopCoroutine(_lowPassCoroutine);
            _lowPassCoroutine = StartCoroutine(TransitionMixerFloat(
                MixerParams.MusicLowPassCutoff, cutoff, lowPassTransitionDuration));
        }

        /// <summary>
        /// Applies a low-pass filter effect on the Master mixer group (affects all sounds).
        /// </summary>
        /// <param name="cutoff">Target cutoff frequency in Hz (10–22000).</param>
        public void SetMasterLowPass(float cutoff)
        {
            if (_lowPassCoroutine != null) StopCoroutine(_lowPassCoroutine);
            _lowPassCoroutine = StartCoroutine(TransitionMixerFloat(
                MixerParams.MasterLowPassCutoff, cutoff, lowPassTransitionDuration));
        }

        /// <summary>Removes the low-pass filter on the Music mixer group (resets to 22000 Hz).</summary>
        public void ClearMusicLowPass() => SetMusicLowPass(22000f);

        /// <summary>Removes the low-pass filter on the Master mixer group (resets to 22000 Hz).</summary>
        public void ClearMasterLowPass() => SetMasterLowPass(22000f);

        // ── Volume Controls ────────────────────────────────────────────────────

        /// <summary>Sets a mixer group volume. Value is in decibels (-80 to 0).</summary>
        public void SetMixerVolume(string paramName, float dB)
        {
            mainMixer.SetFloat(paramName, dB);
        }

        /// <summary>Converts a linear 0–1 volume to decibels and applies it to the given mixer parameter.</summary>
        public void SetMixerVolumeLinear(string paramName, float linear)
        {
            float dB = linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
            SetMixerVolume(paramName, dB);
        }

        // ── Animation Event entry points ───────────────────────────────────────
        // These are string-only overloads so they work with Unity's Animation Event system.

        /// <summary>Animation Event: plays a one-shot sound by ID.</summary>
        public void AnimEvent_PlayOneShot(string soundId) => PlayOneShot(soundId);

        /// <summary>Animation Event: starts a looping sound by ID.</summary>
        public void AnimEvent_PlayLoop(string soundId) => PlayLoop(soundId);

        /// <summary>Animation Event: stops a looping sound by ID.</summary>
        public void AnimEvent_StopLoop(string soundId) => StopLoop(soundId);

        /// <summary>Animation Event: plays music by ID.</summary>
        public void AnimEvent_PlayMusic(string soundId) => PlayMusic(soundId);

        /// <summary>Animation Event: stops the current music.</summary>
        public void AnimEvent_StopMusic(string soundId) => StopMusic();

        // ── Coroutines ─────────────────────────────────────────────────────────

        private IEnumerator CrossfadeMusic(AudioSource outSrc, AudioSource inSrc, float targetVolume)
        {
            float elapsed = 0f;
            float startOut = outSrc.volume;

            while (elapsed < musicFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / musicFadeDuration;
                inSrc.volume  = Mathf.Lerp(0f, targetVolume, t);
                outSrc.volume = Mathf.Lerp(startOut, 0f, t);
                yield return null;
            }

            inSrc.volume  = targetVolume;
            outSrc.volume = 0f;
            outSrc.Stop();
        }

        private IEnumerator FadeOut(AudioSource src)
        {
            float startVolume = src.volume;
            float elapsed = 0f;

            while (elapsed < musicFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(startVolume, 0f, elapsed / musicFadeDuration);
                yield return null;
            }

            src.volume = 0f;
            src.Stop();
        }

        private IEnumerator TransitionMixerFloat(string paramName, float target, float duration)
        {
            mainMixer.GetFloat(paramName, out float current);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float value = Mathf.Lerp(current, target, elapsed / duration);
                mainMixer.SetFloat(paramName, value);
                yield return null;
            }

            mainMixer.SetFloat(paramName, target);
        }
    }
}
