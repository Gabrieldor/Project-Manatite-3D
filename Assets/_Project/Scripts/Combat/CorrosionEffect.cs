using UnityEngine;

namespace Manatite.Combat
{
    /// <summary>
    /// Damage-over-time status: green tint while active, ticks
    /// <see cref="Health.TakeDamage"/> once per second, and kicks a visual "flinch" rotation
    /// on each tick. Added lazily (get-or-add) by <see cref="Manatite.Weapons.ProjectileLauncher"/>
    /// on any hit that carries a corrosion chip's duration/DPS.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class CorrosionEffect : MonoBehaviour
    {
        [Tooltip("Max random rotation offset per axis applied on each damage tick.")]
        [SerializeField] private float jitterDegrees = 8f;
        [Tooltip("Seconds for the tick jitter to ease back to rest pose.")]
        [SerializeField] private float jitterRecoverDuration = 0.15f;

        // The target's actual model shader (a texture-sampling toon graph, shared with the
        // player ship) exposes no tintable color property at all, so MaterialPropertyBlock has
        // nothing to drive. Instead, the tint is a translucent green overlay pass appended as an
        // extra material slot on the same renderer/submesh — works regardless of the target's
        // own shader. One shared instance for the whole game (flat color, no per-target
        // variation needed), built at runtime so no new asset is required.
        private static Material _tintOverlayMaterial;

        private static Material TintOverlayMaterial
        {
            get
            {
                if (_tintOverlayMaterial == null)
                {
                    _tintOverlayMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                    {
                        name = "CorrosionTintOverlay (generated)"
                    };
                    _tintOverlayMaterial.SetFloat("_Surface", 1f); // Transparent
                    _tintOverlayMaterial.SetFloat("_Blend", 0f); // Alpha
                    _tintOverlayMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    _tintOverlayMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    _tintOverlayMaterial.SetFloat("_ZWrite", 0f);
                    _tintOverlayMaterial.SetOverrideTag("RenderType", "Transparent");
                    _tintOverlayMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    _tintOverlayMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    _tintOverlayMaterial.SetColor("_BaseColor", new Color(0.55f, 0.95f, 0.5f, 0.45f));
                }
                return _tintOverlayMaterial;
            }
        }

        private Health _health;
        private Renderer[] _renderers;
        // Enemy prefabs keep their mesh under a "Model" child, separate from the AI-owned root
        // transform (EnemyController drives root rotation every FixedUpdate via Rigidbody) —
        // jittering the root would fight that. Falls back to self if no such child exists.
        private Transform _jitterTransform;
        private Quaternion _restLocalRotation;
        private Quaternion _jitterKickRotation;

        private bool _active;
        private int _dps;
        private float _remainingDuration;
        private float _tickTimer;
        private float _jitterTimer;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _renderers = GetComponentsInChildren<Renderer>();

            Transform model = transform.Find("Model");
            _jitterTransform = model != null ? model : transform;
            _restLocalRotation = _jitterTransform.localRotation;
        }

        /// <summary>Starts (or refreshes) the corrosion status. Re-applying while already active
        /// overwrites dps/duration with the new values rather than stacking.</summary>
        public void Apply(int dps, float duration)
        {
            if (dps <= 0 || duration <= 0f) return;

            _dps = dps;
            _remainingDuration = Mathf.Max(_remainingDuration, duration);

            if (_active) return;
            _active = true;
            _tickTimer = 0f;

            ApplyTint();
        }

        private void Update()
        {
            if (!_active) return;

            if (_jitterTimer > 0f)
            {
                _jitterTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(_jitterTimer / jitterRecoverDuration);
                _jitterTransform.localRotation = Quaternion.Slerp(_jitterKickRotation, _restLocalRotation, t);
            }

            _remainingDuration -= Time.deltaTime;
            _tickTimer += Time.deltaTime;
            if (_tickTimer >= 1f)
            {
                _tickTimer -= 1f;
                Tick();
            }

            if (_remainingDuration <= 0f) EndEffect();
        }

        private void Tick()
        {
            _health.TakeDamage(_dps);

            Vector3 axisJitter = new Vector3(
                Random.Range(-jitterDegrees, jitterDegrees),
                Random.Range(-jitterDegrees, jitterDegrees),
                Random.Range(-jitterDegrees, jitterDegrees));
            _jitterKickRotation = _restLocalRotation * Quaternion.Euler(axisJitter);
            _jitterTransform.localRotation = _jitterKickRotation;
            _jitterTimer = jitterRecoverDuration;
        }

        private void EndEffect()
        {
            _active = false;
            _remainingDuration = 0f;
            _jitterTimer = 0f;
            _jitterTransform.localRotation = _restLocalRotation;
            RevertTint();
        }

        // Pool release (e.g. on death) must fully reset state so a reused pooled enemy never
        // spawns pre-tinted/pre-corroded.
        private void OnDisable()
        {
            if (_active) EndEffect();
        }

        private void ApplyTint()
        {
            Material overlay = TintOverlayMaterial;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null) continue;

                Material[] shared = r.sharedMaterials;
                var withTint = new Material[shared.Length + 1];
                System.Array.Copy(shared, withTint, shared.Length);
                withTint[shared.Length] = overlay;
                r.sharedMaterials = withTint;
            }
        }

        private void RevertTint()
        {
            Material overlay = TintOverlayMaterial;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null) continue;

                Material[] shared = r.sharedMaterials;
                if (shared.Length == 0 || shared[shared.Length - 1] != overlay) continue;

                var withoutTint = new Material[shared.Length - 1];
                System.Array.Copy(shared, withoutTint, withoutTint.Length);
                r.sharedMaterials = withoutTint;
            }
        }
    }
}
