using System;
using UnityEngine;

namespace Manatite.Data
{
    /// <summary>
    /// One playable sound: a clip pool (random variation) plus a volume/pitch range randomized
    /// per play. Leave <see cref="clips"/> empty and playback is silently skipped — the system
    /// is fully wired regardless of whether art has assigned clips yet.
    /// </summary>
    [Serializable]
    public struct AudioCueData
    {
        [SerializeField] private AudioClip[] clips;
        [Tooltip("Multiplies the category (SFX/Music) volume slider. 1 = unchanged.")]
        [SerializeField] private Vector2 volumeRange;
        [SerializeField] private Vector2 pitchRange;

        public bool HasClip => clips != null && clips.Length > 0;

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
