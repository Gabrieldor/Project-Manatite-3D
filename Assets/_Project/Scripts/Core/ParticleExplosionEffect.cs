using UnityEngine;

namespace Manatite.Core
{
    /// <summary>
    /// Pooled, self-timed VFX: a particle burst sized to <see cref="Play"/>'s radius, paired
    /// with a bright point light that flashes and fades out. Alternate splash-damage explosion
    /// effect being tested alongside <see cref="FadingSphereEffect"/> — same Play(radius)
    /// contract so it's a drop-in swap on <c>WeaponData.splashVfxPrefab</c>.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class ParticleExplosionEffect : MonoBehaviour, IRadiusVfx
    {
        [Tooltip("Light intensity per unit of explosion radius, at the moment the burst fires.")]
        [SerializeField] private float lightIntensityPerRadius = 60f;
        [Tooltip("Light range as a multiple of the explosion radius.")]
        [SerializeField] private float lightRangeMultiplier = 3f;
        [Tooltip("Seconds for the light to fade from peak intensity to off.")]
        [SerializeField] private float lightFadeDuration = 0.35f;

        // Under the particle system's default "Shape" scaling mode, GameObject.transform.localScale
        // only resizes the emission shape's spawn volume — it does NOT resize particle start size or
        // speed. Radius must instead drive those main-module values directly so the burst actually
        // grows/shrinks with the explosion, matching how FadingSphereEffect scales its mesh.
        // Tuned (see git history / conversation) so the farthest-traveling particle's distance at
        // end of life equals the explosion radius almost exactly, confirmed by simulation across
        // radius 1-8: maxDist/radius stayed within 1.00-1.01. This keeps the VFX's visible extent
        // truthful to the actual splash-damage hit area (which is chip-configurable), rather than
        // an arbitrary stylistic size — the old FadingSphereEffect had this same guarantee by
        // literally scaling its mesh to radius.
        [Tooltip("Particle start size per unit of explosion radius, at the low/high ends of the random range.")]
        [SerializeField] private Vector2 sizePerRadiusRange = new Vector2(0.22f, 0.42f);
        [Tooltip("Particle start speed per unit of explosion radius, at the low/high ends of the random range. Tuned together with dragLimitPerRadius so the burst's outer edge lands on the explosion radius by the time it fades — retune both together, and re-verify against splash radii the chips can actually roll.")]
        [SerializeField] private Vector2 speedPerRadiusRange = new Vector2(1.85f, 2.15f);
        [Tooltip("Emission shape radius per unit of explosion radius.")]
        [SerializeField] private float shapeRadiusPerRadius = 0.15f;
        [Tooltip("Burst particle count per unit of explosion radius.")]
        [SerializeField] private float particlesPerRadius = 10f;
        [Tooltip("Speed the outward burst decelerates toward (Limit Velocity module), per unit of explosion radius. See speedPerRadiusRange tooltip — tuned as a pair.")]
        [SerializeField] private float dragLimitPerRadius = 1.75f;

        private ParticleSystem _particles;
        private Light _light;
        private float _elapsed;
        private float _peakIntensity;
        private bool _lightFading;

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            _light = GetComponentInChildren<Light>();
        }

        /// <summary>Starts (or restarts) the effect: bursts particles and light scaled to <paramref name="radius"/>.</summary>
        public void Play(float radius)
        {
            ParticleSystem.MainModule main = _particles.main;
            main.startSize = new ParticleSystem.MinMaxCurve(Mathf.Max(0.2f, radius * sizePerRadiusRange.x), Mathf.Max(0.3f, radius * sizePerRadiusRange.y));
            main.startSpeed = new ParticleSystem.MinMaxCurve(Mathf.Max(1f, radius * speedPerRadiusRange.x), Mathf.Max(2f, radius * speedPerRadiusRange.y));

            ParticleSystem.ShapeModule shape = _particles.shape;
            shape.radius = Mathf.Max(0.05f, radius * shapeRadiusPerRadius);

            ParticleSystem.EmissionModule emission = _particles.emission;
            ParticleSystem.Burst burst = emission.GetBurst(0);
            burst.count = new ParticleSystem.MinMaxCurve(Mathf.Max(8, radius * particlesPerRadius));
            emission.SetBurst(0, burst);

            ParticleSystem.LimitVelocityOverLifetimeModule limitVelocity = _particles.limitVelocityOverLifetime;
            limitVelocity.limit = new ParticleSystem.MinMaxCurve(Mathf.Max(0.3f, radius * dragLimitPerRadius));

            _particles.Clear(true);
            _particles.Play(true);

            _elapsed = 0f;
            _lightFading = _light != null;
            if (_light != null)
            {
                _peakIntensity = lightIntensityPerRadius * radius;
                _light.range = radius * lightRangeMultiplier;
                _light.intensity = _peakIntensity;
                _light.enabled = true;
            }
        }

        private void Update()
        {
            if (_lightFading)
            {
                _elapsed += Time.deltaTime;
                float t = _elapsed / lightFadeDuration;
                if (t >= 1f)
                {
                    _light.enabled = false;
                    _lightFading = false;
                }
                else
                {
                    _light.intensity = Mathf.Lerp(_peakIntensity, 0f, t);
                }
            }

            if (!_particles.IsAlive(true))
                ObjectPooler.Release(gameObject);
        }
    }
}
