using System;
using Manatite.Data;
using UnityEngine;

namespace Manatite.Combat
{
    /// <summary>
    /// Generic damageable stat block. Resets to full whenever (re)enabled, so
    /// pooled instances come back to life at full health on reuse.
    /// </summary>
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private HealthData data;

        public event Action<float, float> OnDamaged;
        public event Action OnDied;

        private float _current;

        public float Current => _current;
        public float Max => data != null ? data.MaxHealth : 0f;

        private void OnEnable() => _current = Max;

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || _current <= 0f) return;

            _current = Mathf.Max(_current - amount, 0f);
            OnDamaged?.Invoke(_current, Max);

            if (_current <= 0f) OnDied?.Invoke();
        }
    }
}
