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
        // Lead time before the first PlayScheduled call, and how far ahead of a loop
        // boundary the next segment gets scheduled — both must clear one frame's worth of
        // Update() latency so the dspTime-scheduled handoff never lands late.
        private const double ScheduleLeadSeconds = 0.05;
        private const double ScheduleLookaheadSeconds = 0.25;

        [SerializeField] private AudioLibraryData library;
        [Tooltip("Pre-allocated AudioSources for overlapping one-shot SFX. Raise if shots ever cut each other off.")]
        [SerializeField] private int sfxVoiceCount = 8;
        [Tooltip("Pre-allocated AudioSource pairs for concurrent intro+loop SFX (see PlayLoopingSfx). Raise if loops ever cut each other off.")]
        [SerializeField] private int loopVoiceCount = 4;
        [SerializeField] private float musicFadeSeconds = 1.5f;

        private static AudioManager _instance;
        private static float _musicVolume01 = -1f; // -1 = not yet loaded from PlayerPrefs
        private static float _sfxVolume01 = -1f;

        private AudioSource[] _sfxVoices;
        private int _nextVoice;
        private LoopVoice[] _loopVoices;
        private int _nextLoopVoice;
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

            _loopVoices = new LoopVoice[Mathf.Max(0, loopVoiceCount)];
            for (int i = 0; i < _loopVoices.Length; i++)
            {
                _loopVoices[i] = new LoopVoice
                {
                    sourceA = CreateSource($"LoopVoice{i}A", loop: false),
                    sourceB = CreateSource($"LoopVoice{i}B", loop: false),
                };
            }

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

            UpdateLoopVoices();
        }

        private void UpdateLoopVoices()
        {
            if (_loopVoices == null) return;
            double now = AudioSettings.dspTime;

            for (int i = 0; i < _loopVoices.Length; i++)
            {
                LoopVoice voice = _loopVoices[i];
                if (!voice.active) continue;

                float vol = voice.baseVolume * voice.volumeScale * SfxVolume01;
                voice.sourceA.volume = vol;
                voice.sourceB.volume = vol;

                if (voice.followTarget != null)
                {
                    voice.sourceA.transform.position = voice.followTarget.position;
                    voice.sourceB.transform.position = voice.followTarget.position;
                }

                if (!voice.hasLoopRegion) continue;

                // Boundary already crossed: the pre-scheduled source is now the one sounding.
                if (voice.playingNext != null && now >= voice.nextBoundaryDspTime)
                {
                    voice.playingNow = voice.playingNext;
                    voice.playingNext = null;
                    voice.nextBoundaryDspTime = voice.pendingBoundary;
                }

                // Approaching the next boundary with nothing queued yet: schedule the other
                // source's next loop pass now so the handoff lands on the exact sample.
                if (voice.playingNext == null && now >= voice.nextBoundaryDspTime - ScheduleLookaheadSeconds)
                {
                    AudioSource next = voice.playingNow == voice.sourceA ? voice.sourceB : voice.sourceA;
                    next.time = voice.loopStart;
                    next.PlayScheduled(voice.nextBoundaryDspTime);
                    next.SetScheduledEndTime(voice.nextBoundaryDspTime + voice.loopDuration);
                    voice.playingNext = next;
                    voice.pendingBoundary = voice.nextBoundaryDspTime + voice.loopDuration;
                }
            }
        }

        private AudioSource CreateSource(string name, bool loop)
        {
            var host = new GameObject(name);
            host.transform.SetParent(transform, false);
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            // Only takes effect on spatial plays (spatialBlend set to 1 per-call). Custom
            // curve instead of the built-in Logarithmic mode: the built-in curve's shape
            // isn't a simple minDistance/distance ratio (confirmed by ear, not just formula),
            // so distance/volume pairs can't be reasoned about analytically — pin them
            // explicitly instead. Full volume out to ~10% of maxDistance (enemies engage from
            // EnemyData.StopDistance (15+) outward, so close-range SFX must stay unattenuated),
            // then a straight line down to exactly 75% at maxDistance (780 — the observed
            // ship-to-surface distance of a planet dead ahead in testing).
            source.rolloffMode = AudioRolloffMode.Custom;
            source.minDistance = 15f;
            source.maxDistance = 780f;
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(
                new Keyframe(0f, 1f, 0f, 0f),
                new Keyframe(0.1f, 1f, 0f, -0.2777778f),
                new Keyframe(1f, 0.75f, -0.2777778f, 0f)));
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

        /// <summary>Plays a loopable cue: 0 to its loop-start plays once, then the loop-start/
        /// loop-end region repeats seamlessly until <see cref="StopLoopingSfx"/> is called.
        /// Cues with no loop region configured just loop the whole clip. Returns a handle to
        /// stop this specific instance; an invalid handle means nothing was played (missing
        /// cue/clip). <paramref name="followTarget"/> is optional — set it to keep a moving
        /// emitter's position current each frame, e.g. an enemy engine loop.</summary>
        public static LoopingSfxHandle PlayLoopingSfx(SfxCue cue, Vector3 worldPosition, bool spatial, float volumeScale = 1f, Transform followTarget = null) =>
            _instance != null ? _instance.PlayLoopingSfxInternal(cue, worldPosition, spatial, volumeScale, followTarget) : LoopingSfxHandle.Invalid;

        public static void StopLoopingSfx(LoopingSfxHandle handle) => _instance?.StopLoopingSfxInternal(handle);

        private LoopingSfxHandle PlayLoopingSfxInternal(SfxCue cue, Vector3 worldPosition, bool spatial, float volumeScale, Transform followTarget)
        {
            if (library == null || !library.TryGetSfx(cue, out AudioCueData data) || !data.HasClip || _loopVoices.Length == 0)
                return LoopingSfxHandle.Invalid;

            int index = _nextLoopVoice;
            _nextLoopVoice = (_nextLoopVoice + 1) % _loopVoices.Length;

            LoopVoice voice = _loopVoices[index];
            voice.sourceA.Stop();
            voice.sourceB.Stop();
            voice.generation++;
            voice.active = true;
            voice.followTarget = followTarget;
            voice.baseVolume = data.GetVolume();
            voice.volumeScale = volumeScale;

            float pitch = data.GetPitch();
            float vol = voice.baseVolume * volumeScale * SfxVolume01;
            AudioClip clip = data.GetClip();

            voice.sourceA.clip = clip;
            voice.sourceB.clip = clip;
            voice.sourceA.pitch = pitch;
            voice.sourceB.pitch = pitch;
            voice.sourceA.spatialBlend = spatial ? 1f : 0f;
            voice.sourceB.spatialBlend = spatial ? 1f : 0f;
            voice.sourceA.transform.position = worldPosition;
            voice.sourceB.transform.position = worldPosition;
            voice.sourceA.volume = vol;
            voice.sourceB.volume = vol;

            if (!data.HasLoopRegion)
            {
                voice.hasLoopRegion = false;
                voice.sourceA.loop = true;
                voice.sourceA.time = 0f;
                voice.sourceA.Play();
                voice.playingNow = voice.sourceA;
                voice.playingNext = null;
            }
            else
            {
                voice.hasLoopRegion = true;
                voice.sourceA.loop = false;
                voice.sourceB.loop = false;
                voice.loopStart = data.LoopStart;
                voice.loopDuration = (data.LoopEnd - data.LoopStart) / pitch;

                double t0 = AudioSettings.dspTime + ScheduleLeadSeconds;
                double introDuration = data.LoopStart / pitch;

                voice.sourceA.time = 0f;
                voice.sourceA.PlayScheduled(t0);
                voice.sourceA.SetScheduledEndTime(t0 + introDuration);

                voice.sourceB.time = data.LoopStart;
                voice.sourceB.PlayScheduled(t0 + introDuration);
                voice.sourceB.SetScheduledEndTime(t0 + introDuration + voice.loopDuration);

                voice.playingNow = voice.sourceA;
                voice.playingNext = voice.sourceB;
                voice.nextBoundaryDspTime = t0 + introDuration;
                voice.pendingBoundary = t0 + introDuration + voice.loopDuration;
            }

            return new LoopingSfxHandle(index, voice.generation);
        }

        private void StopLoopingSfxInternal(LoopingSfxHandle handle)
        {
            if (!handle.IsValid || _loopVoices == null || handle.VoiceIndex >= _loopVoices.Length) return;

            LoopVoice voice = _loopVoices[handle.VoiceIndex];
            if (!voice.active || voice.generation != handle.Generation) return;

            voice.sourceA.Stop();
            voice.sourceB.Stop();
            voice.active = false;
            voice.playingNow = null;
            voice.playingNext = null;
            voice.followTarget = null;
        }

        private sealed class LoopVoice
        {
            public AudioSource sourceA;
            public AudioSource sourceB;
            public AudioSource playingNow;
            public AudioSource playingNext;
            public bool active;
            public bool hasLoopRegion;
            public int generation;
            public float loopStart;
            public float loopDuration;
            public float baseVolume;
            public float volumeScale;
            public Transform followTarget;
            public double nextBoundaryDspTime;
            public double pendingBoundary;
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
