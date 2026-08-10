using UnityEngine;

namespace Manatite.Core
{
    /// <summary>
    /// Pooled, self-timed VFX: a sphere sized to <see cref="Play"/>'s radius that grows from
    /// nothing to full size, then fades to transparent and releases itself back to the
    /// <see cref="ObjectPooler"/>. Used for the splash-damage chip's explosion, but generic
    /// enough for any other radius-based effect.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class FadingSphereEffect : MonoBehaviour
    {
        // Driven on both channels every frame so this works against either a plain Color-based
        // shader (_BaseColor.a) or a shader exposing a dedicated scalar alpha (_Alpha, e.g. the
        // toon/bloom shader graph, which has no alpha channel on its own Color property).
        // MaterialPropertyBlock silently ignores a property name the active shader doesn't have.
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        [Tooltip("Seconds to grow from nothing to full size.")]
        [SerializeField] private float growDuration = 0.15f;
        [Tooltip("Seconds to fade from full opacity to fully transparent, once fully grown.")]
        [SerializeField] private float fadeDuration = 0.25f;
        [Tooltip("RGB tint for shaders that read a single _BaseColor (ignored by shaders without that property). Alpha is controlled by the material's own _Alpha/_BaseColor.a value, not this field.")]
        [SerializeField] private Color color = new Color(1f, 0.55f, 0.15f, 1f);

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private float _targetScale;
        private float _elapsed;
        private float _peakAlpha = 1f;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        /// <summary>Starts (or restarts) the effect: grows to match <paramref name="radius"/>, then fades.</summary>
        public void Play(float radius)
        {
            // Assumes a unit-diameter primitive sphere (radius 0.5 at scale 1).
            _targetScale = radius * 2f;
            _elapsed = 0f;
            transform.localScale = Vector3.zero;
            // The material's own alpha (tuned in its Inspector, e.g. the toon shader's _Alpha
            // slider) is the peak the grow/fade animation multiplies against — read fresh each
            // Play() so live-tweaking the material while iterating takes effect immediately,
            // rather than baking it once at pool-instantiation time.
            _peakAlpha = ReadPeakAlpha();
            ApplyAlpha(1f);
        }

        private float ReadPeakAlpha()
        {
            Material mat = _renderer.sharedMaterial;
            if (mat == null) return 1f;
            if (mat.HasProperty(AlphaId)) return mat.GetFloat(AlphaId);
            if (mat.HasProperty(BaseColorId)) return mat.GetColor(BaseColorId).a;
            return 1f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (_elapsed < growDuration)
            {
                transform.localScale = Vector3.one * Mathf.Lerp(0f, _targetScale, _elapsed / growDuration);
                ApplyAlpha(1f);
                return;
            }

            transform.localScale = Vector3.one * _targetScale;

            float fadeT = (_elapsed - growDuration) / fadeDuration;
            if (fadeT >= 1f)
            {
                ObjectPooler.Release(gameObject);
                return;
            }

            ApplyAlpha(1f - fadeT);
        }

        private void ApplyAlpha(float alpha01)
        {
            float a = _peakAlpha * alpha01;
            _renderer.GetPropertyBlock(_block);
            Color c = color;
            c.a = a;
            _block.SetColor(BaseColorId, c);
            _block.SetFloat(AlphaId, a);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
