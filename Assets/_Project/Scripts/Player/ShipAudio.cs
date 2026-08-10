using Manatite.Audio;
using Manatite.Data;
using UnityEngine;

namespace Manatite.Player
{
    /// <summary>
    /// Loops the <see cref="SfxCue.ThrusterLoop"/> cue while enabled, with volume following the
    /// ship's current speed as a fraction of its top speed — movement's audio trigger, always
    /// playing, just quiet at a standstill.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class ShipAudio : MonoBehaviour
    {
        [SerializeField] private Rigidbody shipRigidbody;
        [Tooltip("Same asset as the ship's ShipController, reused here for MaxSpeed.")]
        [SerializeField] private ShipMovementData data;
        [Tooltip("Fraction of the cue's base volume while stationary (0 = silent at rest).")]
        [Range(0f, 1f)] [SerializeField] private float minVolumeFraction = 0.15f;
        [Tooltip("Used when data.MaxSpeed is 0 (unlimited).")]
        [SerializeField] private float fallbackMaxSpeed = 40f;

        private AudioSource _source;
        private float _cueBaseVolume = 1f;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;

            if (AudioManager.TryGetSfxClip(SfxCue.ThrusterLoop, out AudioClip clip, out float volume, out float pitch))
            {
                _source.clip = clip;
                _source.pitch = pitch;
                _cueBaseVolume = volume;
                _source.Play();
            }
        }

        private void Update()
        {
            if (_source.clip == null || shipRigidbody == null) return;

            float maxSpeed = data != null && data.MaxSpeed > 0f ? data.MaxSpeed : fallbackMaxSpeed;
            float speedFraction = Mathf.Clamp01(shipRigidbody.linearVelocity.magnitude / maxSpeed);
            _source.volume = _cueBaseVolume * Mathf.Lerp(minVolumeFraction, 1f, speedFraction) * AudioManager.SfxVolume01;
        }
    }
}
