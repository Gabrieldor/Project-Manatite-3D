using System.Collections;
using Manatite.Data;
using UnityEngine;

namespace Manatite.Audio
{
    /// <summary>
    /// Central SFX/music playback. One instance lives in each scene (Main Menu, Outer Space),
    /// wired to the shared <see cref="AudioLibraryData"/> asset. Not persisted across scene
    /// loads — every scene sets its own music on entry (see
    /// <see cref="Manatite.UI.MainMenu"/>, <see cref="MusicDirector"/>) and the volume sliders
    /// are cached in static fields (backed by <see cref="PlayerPrefs"/>) that outlive any one
    /// instance. SFX one-shots play through a fixed pool of pre-allocated <see cref="AudioSource"/>s
    /// — no Instantiate/Destroy per shot.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const string MusicVolumeKey = "Audio.MusicVolume";
        private const string SfxVolumeKey = "Audio.SfxVolume";

        [SerializeField] private AudioLibraryData library;
        [Tooltip("Pre-allocated AudioSources for overlapping one-shot SFX. Raise if shots ever cut each other off.")]
        [SerializeField] private int sfxVoiceCount = 8;
        [SerializeField] private float musicFadeSeconds = 1.5f;

        private static AudioManager _instance;
        private static float _musicVolume01 = -1f; // -1 = not yet loaded from PlayerPrefs
        private static float _sfxVolume01 = -1f;

        private AudioSource[] _sfxVoices;
        private int _nextVoice;
        private AudioSource _musicSourceA;
        private AudioSource _musicSourceB;
        private AudioSource _activeMusicSource;
        private float _activeMusicBaseVolume;
        private MusicTrack _currentTrack = MusicTrack.None;
        private Coroutine _musicFadeRoutine;

        public static float MusicVolume01
        {
            get => LoadedMusicVolume;
            set
            {
                _musicVolume01 = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MusicVolumeKey, _musicVolume01);
            }
        }

        public static float SfxVolume01
        {
            get => LoadedSfxVolume;
            set
            {
                _sfxVolume01 = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(SfxVolumeKey, _sfxVolume01);
            }
        }

        private static float LoadedMusicVolume
        {
            get
            {
                if (_musicVolume01 < 0f) _musicVolume01 = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
                return _musicVolume01;
            }
        }

        private static float LoadedSfxVolume
        {
            get
            {
                if (_sfxVolume01 < 0f) _sfxVolume01 = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
                return _sfxVolume01;
            }
        }

        private void Awake()
        {
            _instance = this;

            _sfxVoices = new AudioSource[Mathf.Max(1, sfxVoiceCount)];
            for (int i = 0; i < _sfxVoices.Length; i++)
                _sfxVoices[i] = CreateSource($"SfxVoice{i}", loop: false);

            _musicSourceA = CreateSource("MusicA", loop: true);
            _musicSourceB = CreateSource("MusicB", loop: true);
            _activeMusicSource = _musicSourceA;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            // Keeps the currently playing track's volume in sync with slider drags that
            // happen while no fade is in flight (fades apply the slider live on their own).
            if (_musicFadeRoutine == null && _activeMusicSource != null && _activeMusicSource.isPlaying)
                _activeMusicSource.volume = _activeMusicBaseVolume * MusicVolume01;
        }

        private AudioSource CreateSource(string name, bool loop)
        {
            var host = new GameObject(name);
            host.transform.SetParent(transform, false);
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        /// <summary>Non-spatial one-shot — always equally audible. Use for player-sourced SFX
        /// (own gun, own hits) and hit-marker-style feedback.</summary>
        public static void PlaySfx(SfxCue cue, float volumeScale = 1f) =>
            _instance?.PlaySfxInternal(cue, Vector3.zero, spatial: false, volumeScale);

        /// <summary>Spatial one-shot at a world position — falls off with distance. Use for
        /// enemy-sourced SFX so the player can hear where they're coming from.</summary>
        public static void PlaySfx3D(SfxCue cue, Vector3 worldPosition, float volumeScale = 1f) =>
            _instance?.PlaySfxInternal(cue, worldPosition, spatial: true, volumeScale);

        private void PlaySfxInternal(SfxCue cue, Vector3 worldPosition, bool spatial, float volumeScale)
        {
            if (library == null || !library.TryGetSfx(cue, out AudioCueData data) || !data.HasClip) return;

            AudioSource source = _sfxVoices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _sfxVoices.Length;

            source.transform.position = worldPosition;
            source.spatialBlend = spatial ? 1f : 0f;
            source.pitch = data.GetPitch();
            source.volume = data.GetVolume() * volumeScale * SfxVolume01;
            source.clip = data.GetClip();
            source.Play();
        }

        /// <summary>Looks up a cue's clip/volume/pitch without playing it — for a continuous
        /// sound a caller drives itself (e.g. <see cref="Manatite.Player.ShipAudio"/>'s
        /// thruster loop).</summary>
        public static bool TryGetSfxClip(SfxCue cue, out AudioClip clip, out float volume, out float pitch)
        {
            if (_instance != null && _instance.library != null && _instance.library.TryGetSfx(cue, out AudioCueData data) && data.HasClip)
            {
                clip = data.GetClip();
                volume = data.GetVolume();
                pitch = data.GetPitch();
                return true;
            }

            clip = null;
            volume = 0f;
            pitch = 1f;
            return false;
        }

        public static void PlayMusic(MusicTrack track) => _instance?.PlayMusicInternal(track);

        private void PlayMusicInternal(MusicTrack track)
        {
            if (track == _currentTrack) return;
            _currentTrack = track;

            if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);

            if (library == null || track == MusicTrack.None || !library.TryGetMusic(track, out AudioClip clip, out float baseVolume) || clip == null)
            {
                _musicFadeRoutine = StartCoroutine(FadeOutAndStop(_activeMusicSource));
                return;
            }

            AudioSource incoming = _activeMusicSource == _musicSourceA ? _musicSourceB : _musicSourceA;
            AudioSource outgoing = _activeMusicSource;
            incoming.clip = clip;
            incoming.volume = 0f;
            incoming.Play();
            _activeMusicSource = incoming;
            _activeMusicBaseVolume = baseVolume;

            _musicFadeRoutine = StartCoroutine(CrossfadeMusic(incoming, outgoing, baseVolume));
        }

        private IEnumerator CrossfadeMusic(AudioSource incoming, AudioSource outgoing, float baseVolume)
        {
            bool outgoingWasPlaying = outgoing.isPlaying;
            float t = 0f;
            while (t < musicFadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                float f = musicFadeSeconds <= 0f ? 1f : Mathf.Clamp01(t / musicFadeSeconds);
                incoming.volume = baseVolume * MusicVolume01 * f;
                if (outgoingWasPlaying) outgoing.volume = baseVolume * MusicVolume01 * (1f - f);
                yield return null;
            }

            incoming.volume = baseVolume * MusicVolume01;
            if (outgoingWasPlaying) outgoing.Stop();
            _musicFadeRoutine = null;
        }

        private IEnumerator FadeOutAndStop(AudioSource source)
        {
            float startVolume = source.volume;
            float t = 0f;
            while (t < musicFadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, musicFadeSeconds <= 0f ? 1f : t / musicFadeSeconds);
                yield return null;
            }

            source.Stop();
            _musicFadeRoutine = null;
        }
    }
}
