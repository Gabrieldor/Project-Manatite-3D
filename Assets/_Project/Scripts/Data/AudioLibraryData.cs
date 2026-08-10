using System;
using Manatite.Audio;
using UnityEngine;

namespace Manatite.Data
{
    [Serializable]
    public struct SfxLibraryEntry
    {
        public SfxCue cue;
        public AudioCueData data;
    }

    [Serializable]
    public struct MusicLibraryEntry
    {
        public MusicTrack track;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    /// <summary>
    /// Single shared asset mapping every <see cref="SfxCue"/>/<see cref="MusicTrack"/> to its
    /// clip(s). The one place to (re)configure what plays for a given trigger — see
    /// <see cref="Manatite.Audio.AudioManager"/> for playback.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Manatite/Audio/Audio Library", order = 10)]
    public sealed class AudioLibraryData : ScriptableObject
    {
        [SerializeField] private SfxLibraryEntry[] sfxCues;
        [SerializeField] private MusicLibraryEntry[] musicTracks;

        public bool TryGetSfx(SfxCue cue, out AudioCueData data)
        {
            if (sfxCues != null)
            {
                for (int i = 0; i < sfxCues.Length; i++)
                {
                    if (sfxCues[i].cue != cue) continue;
                    data = sfxCues[i].data;
                    return true;
                }
            }

            data = default;
            return false;
        }

        public bool TryGetMusic(MusicTrack track, out AudioClip clip, out float volume)
        {
            if (musicTracks != null)
            {
                for (int i = 0; i < musicTracks.Length; i++)
                {
                    if (musicTracks[i].track != track) continue;
                    clip = musicTracks[i].clip;
                    volume = musicTracks[i].volume == 0f ? 1f : musicTracks[i].volume;
                    return true;
                }
            }

            clip = null;
            volume = 0f;
            return false;
        }
    }
}
