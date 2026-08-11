using UnityEngine;

namespace Manatite.Core
{
    /// <summary>
    /// Pooled, one-shot lightning bolt between two points: stretches a particle emission shape
    /// (local Z axis) to span the distance and flashes a child <see cref="Light"/> from peak
    /// intensity to zero over the burst. Self-releases back to the <see cref="ObjectPooler"/>
    /// once the particle system finishes, same mechanism as <see cref="HitMarkerEffect"/>.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class LightningArcEffect : MonoBehaviour
    {
        [Tooltip("Flashed from lightIntensity down to 0 over the particle system's burst duration.")]
        [SerializeField] private Light boltLight;
        [SerializeField] private float lightIntensity = 4f;

        private ParticleSystem _particles;
        private ParticleSystem.ShapeModule _shape;
        private float _burstDuration;
        private float _elapsed;
        private bool _lightActive;

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            _shape = _particles.shape;

            ParticleSystem.MainModule main = _particles.main;
            main.stopAction = ParticleSystemStopAction.Callback;
            _burstDuration = main.duration;
        }

        /// <summary>Orients and stretches the bolt between the two points, then plays the burst.</summary>
        public void Play(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            float distance = offset.magnitude;

            transform.position = from;
            transform.rotation = distance > 0.001f ? Quaternion.LookRotation(offset) : Quaternion.identity;
            _shape.scale = new Vector3(_shape.scale.x, _shape.scale.y, distance);

            _elapsed = 0f;
            _lightActive = boltLight != null;
            if (_lightActive) boltLight.intensity = lightIntensity;

            _particles.Clear(true);
            _particles.Play(true);
        }

        private void Update()
        {
            if (!_lightActive) return;

            _elapsed += Time.deltaTime;
            float t = _burstDuration > 0f ? Mathf.Clamp01(_elapsed / _burstDuration) : 1f;
            boltLight.intensity = Mathf.Lerp(lightIntensity, 0f, t);

            if (t >= 1f) _lightActive = false;
        }

        // Unity message: invoked automatically when the system's main module has
        // stopAction = Callback and it finishes (all particles dead, not looping).
        private void OnParticleSystemStopped()
        {
            if (boltLight != null) boltLight.intensity = 0f;
            _lightActive = false;
            ObjectPooler.Release(gameObject);
        }
    }
}
