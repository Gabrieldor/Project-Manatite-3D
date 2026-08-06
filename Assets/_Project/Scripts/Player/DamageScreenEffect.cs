using Manatite.Combat;
using Manatite.Data;
using UnityEngine;

namespace Manatite.Player
{
    /// <summary>
    /// Drives the fullscreen broken-glass crack material's damage amount from the
    /// player's <see cref="Health"/>, so the camera looks more cracked as HP drops.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class DamageScreenEffect : MonoBehaviour
    {
        private static readonly int DamageId = Shader.PropertyToID("_Damage");

        [SerializeField] private Material crackMaterial;
        [SerializeField] private DamageScreenEffectData data;

        private Health _health;

        private void Awake() => _health = GetComponent<Health>();

        private void OnEnable() => _health.OnDamaged += HandleDamaged;

        private void OnDisable()
        {
            _health.OnDamaged -= HandleDamaged;
            if (crackMaterial != null) crackMaterial.SetFloat(DamageId, 0f);
        }

        // Start (not OnEnable) so this reads Health.Current after Health's own OnEnable
        // has reset it to Max — sibling OnEnable order isn't guaranteed, but Start always
        // runs after every object's OnEnable in the same activation.
        private void Start() => ApplyDamage(_health.Current, _health.Max);

        private void HandleDamaged(int current, int max) => ApplyDamage(current, max);

        private void ApplyDamage(int current, int max)
        {
            if (crackMaterial == null || max <= 0) return;

            float healthPercent = (float)current / max;
            float intensity = data != null ? data.Evaluate(healthPercent) : 1f - healthPercent;
            crackMaterial.SetFloat(DamageId, intensity);
        }
    }
}
