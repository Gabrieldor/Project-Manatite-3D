using System;
using UnityEngine;

namespace Manatite.Data
{
    /// <summary>
    /// One playable sound: a clip pool (random variation) plus a volume/pitch range randomized
    /// per play. Leave <see cref="clips"/> empty and playback is silently skipped — the system
    /// is fully wired regardless of whether art has assigned clips yet.
    /// Optionally loopable via <see cref="Manatite.Audio.AudioManager.PlayLoopingSfx"/>: with
    /// <see cref="loop"/> off, or on with <see cref="loopEnd"/> left at/below
    /// <see cref="loopStart"/>, that plays the whole clip on repeat. With <see cref="loop"/> on
    /// and a valid region, 0 to loopStart plays once, then [loopStart, loopEnd) repeats
    /// seamlessly — loop points are set with a slider in the Inspector, bounded to clip 0's
    /// length.
    /// </summary>
    [Serializable]
    public struct AudioCueData
    {
        [SerializeField] private AudioClip[] clips;
        [Tooltip("Multiplies the category (SFX/Music) volume slider. 1 = unchanged.")]
        [SerializeField] private Vector2 volumeRange;
        [SerializeField] private Vector2 pitchRange;
        [Tooltip("Loop this cue when played via PlayLoopingSfx. With a region set below, the intro (0 to loop start) plays once before the loop region repeats.")]
        [SerializeField] private bool loop;
        [Tooltip("Seconds, bounded to clip 0's length. Leave loopEnd at/below loopStart to loop the whole clip.")]
        [SerializeField] private float loopStart;
        [SerializeField] private float loopEnd;

        public bool HasClip => clips != null && clips.Length > 0;
        public bool Loop => loop;
        public bool HasLoopRegion => loop && loopEnd > loopStart;
        public float LoopStart => loopStart;
        public float LoopEnd => loopEnd;

        public AudioClip GetClip()
        {
            if (!HasClip) return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        // A freshly-added array element defaults every field to zero (structs run no field
        // initializers), so an unconfigured range reads as (0,0) rather than the sensible
        // "unchanged" value — treat that as 1 instead of silently playing at zero volume/pitch.
        public float GetVolume() => volumeRange == Vector2.zero ? 1f : UnityEngine.Random.Range(volumeRange.x, volumeRange.y);
        public float GetPitch() => pitchRange == Vector2.zero ? 1f : UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
    }
}
