using Manatite.Audio;
using UnityEngine;

namespace Manatite.Player
{
    /// <summary>
    /// Plays the <see cref="SfxCue.ThrusterLoop"/> cue from one of six positioned mounts
    /// depending on which way the ship is being pushed — e.g. strafing left fires the
    /// right-side mount, since a thruster expels mass opposite the direction of motion.
    /// Each mount is an independent spatialized channel: several can play at once (e.g.
    /// strafing while climbing), each fading in/out with its own axis's input strength.
    /// </summary>
    public sealed class ShipAudio : MonoBehaviour
    {
        private const int Left = 0;
        private const int Right = 1;
        private const int Top = 2;
        private const int Bottom = 3;
        private const int Front = 4;
        private const int Rear = 5;
        private const int ChannelCount = 6;

        [Tooltip("Defaults to this GameObject's ShipController if left unassigned.")]
        [SerializeField] private ShipController shipController;
        [Header("Directional Thruster Mounts")]
        [SerializeField] private Transform leftMount;
        [SerializeField] private Transform rightMount;
        [SerializeField] private Transform topMount;
        [SerializeField] private Transform bottomMount;
        [SerializeField] private Transform frontMount;
        [SerializeField] private Transform rearMount;
        [Header("Mix")]
        [Tooltip("Fraction of the cue's base volume at the lightest detectable stick push.")]
        [Range(0f, 1f)] [SerializeField] private float minVolumeFraction = 0.15f;
        [Tooltip("Stick/axis magnitude below this counts as no input (dead zone).")]
        [SerializeField] private float inputThreshold = 0.01f;
        [Tooltip("How long a channel takes to fade to silence after its input stops, before it actually stops playing.")]
        [SerializeField] private float fadeOutSeconds = 0.15f;

        private struct ThrusterChannel
        {
            public AudioSource source;
            public float fadeTimer; // -1 = not fading
            public float fadeStartVolume;
        }

        private ThrusterChannel[] _channels;
        private float _cueBaseVolume = 1f;

        private void Awake()
        {
            if (shipController == null) shipController = GetComponent<ShipController>();

            AudioManager.TryGetSfxClip(SfxCue.ThrusterLoop, out AudioClip clip, out float volume, out float pitch);
            _cueBaseVolume = volume;

            _channels = new ThrusterChannel[ChannelCount];
            SetupChannel(Left, leftMount, clip, pitch);
            SetupChannel(Right, rightMount, clip, pitch);
            SetupChannel(Top, topMount, clip, pitch);
            SetupChannel(Bottom, bottomMount, clip, pitch);
            SetupChannel(Front, frontMount, clip, pitch);
            SetupChannel(Rear, rearMount, clip, pitch);
        }

        private void SetupChannel(int index, Transform mount, AudioClip clip, float pitch)
        {
            _channels[index].fadeTimer = -1f;
            if (mount == null) return;

            AudioSource source = mount.GetComponent<AudioSource>();
            if (source == null) return;

            source.playOnAwake = false;
            source.loop = true;
            source.clip = clip;
            source.pitch = pitch;
            _channels[index].source = source;
        }

        private void Update()
        {
            if (shipController == null) return;

            Vector2 move = shipController.MoveInput;
            float elevate = shipController.ElevateInput;

            // Each mount fires opposite its axis's direction of motion (reaction mass pushes
            // the ship the other way) — e.g. strafing right (+move.x) fires the left mount.
            UpdateChannel(Left, move.x);
            UpdateChannel(Right, -move.x);
            UpdateChannel(Bottom, elevate);
            UpdateChannel(Top, -elevate);
            UpdateChannel(Rear, move.y);
            UpdateChannel(Front, -move.y);
        }

        private void UpdateChannel(int index, float rawActivation)
        {
            AudioSource source = _channels[index].source;
            if (source == null || source.clip == null) return;

            float activation = Mathf.Clamp01(rawActivation);

            if (activation > inputThreshold)
            {
                _channels[index].fadeTimer = -1f;
                if (!source.isPlaying) source.Play();
                source.volume = _cueBaseVolume * Mathf.Lerp(minVolumeFraction, 1f, activation) * AudioManager.SfxVolume01;
                return;
            }

            if (!source.isPlaying) return;

            if (_channels[index].fadeTimer < 0f)
            {
                _channels[index].fadeTimer = 0f;
                _channels[index].fadeStartVolume = source.volume;
            }

            _channels[index].fadeTimer += Time.deltaTime;
            // Drive volume from a clamped fade fraction so it lands exactly on 0 on the same
            // frame Stop() fires — cutting mid-waveform at any non-zero amplitude clicks.
            float fadeFraction = fadeOutSeconds > 0f ? Mathf.Clamp01(_channels[index].fadeTimer / fadeOutSeconds) : 1f;
            source.volume = Mathf.Lerp(_channels[index].fadeStartVolume, 0f, fadeFraction);

            if (fadeFraction >= 1f)
            {
                source.Stop();
                _channels[index].fadeTimer = -1f;
            }
        }
    }
}
