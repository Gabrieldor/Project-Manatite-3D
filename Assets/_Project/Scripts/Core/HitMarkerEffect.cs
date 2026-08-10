using UnityEngine;

namespace Manatite.Core
{
    /// <summary>
    /// Pooled, one-shot particle burst marking where a shot landed. <see cref="Play"/> restarts
    /// the burst; the system self-releases back to the <see cref="ObjectPooler"/> once it
    /// finishes, via <see cref="ParticleSystemStopAction.Callback"/>.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class HitMarkerEffect : MonoBehaviour
    {
        private ParticleSystem _particles;

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = _particles.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        public void Play()
        {
            _particles.Clear(true);
            _particles.Play(true);
        }

        // Unity message: invoked automatically when the system's main module has
        // stopAction = Callback and it finishes (all particles dead, not looping).
        private void OnParticleSystemStopped()
        {
            ObjectPooler.Release(gameObject);
        }
    }
}
