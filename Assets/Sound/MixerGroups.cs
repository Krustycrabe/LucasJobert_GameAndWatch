namespace GameAndWatch.Audio
{
    /// <summary>
    /// Constants matching the AudioMixer group names defined in MainMixer.mixer.
    /// Update these if you rename groups in the mixer asset.
    /// </summary>
    public static class MixerGroups
    {
        public const string Master = "Master";
        public const string SFX    = "SFX";
        public const string Loop   = "Loop";
        public const string Music  = "Music";
    }

    /// <summary>
    /// Exposed parameter names on the MainMixer asset.
    /// These must match exactly the "Exposed Parameters" defined in the AudioMixer Inspector.
    /// </summary>
    public static class MixerParams
    {
        public const string MasterVolume = "MasterVolume";
        public const string SFXVolume    = "SFXVolume";
        public const string LoopVolume   = "LoopVolume";
        public const string MusicVolume  = "MusicVolume";

        // Low-pass filter cutoff exposed on the Music group send
        public const string MusicLowPassCutoff = "MusicLowPassCutoff";
        // Low-pass filter cutoff exposed on the Master group (affects all sounds)
        public const string MasterLowPassCutoff = "MasterLowPassCutoff";
    }
}
