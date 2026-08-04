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

        public event Action<int, int> OnDamaged;
        public event Action OnDied;

        private int _current;
        private int? _maxHealthOverride;

        public int Current => _current;
        public int Max => _maxHealthOverride ?? (data != null ? data.MaxHealth : 0);

        /// <summary>Lets a caller (e.g. <see cref="Manatite.Enemies.EnemyController"/>) drive max
        /// health from its own data asset instead of assigning a <see cref="HealthData"/> here.</summary>
        public void SetMaxHealth(int value) => _maxHealthOverride = value;

        /// <summary>Sets current HP directly (e.g. restoring from a save), clamped to [0, Max].</summary>
        public void SetCurrent(int value) => _current = Mathf.Clamp(value, 0, Max);

        private void OnEnable() => _current = Max;

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || _current <= 0) return;

            _current = Mathf.Max(_current - amount, 0);
            OnDamaged?.Invoke(_current, Max);

            if (_current <= 0) OnDied?.Invoke();
        }
    }
}
