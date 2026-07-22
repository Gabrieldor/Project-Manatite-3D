using Manatite.Data;
using Manatite.Weapons;
using UnityEngine;

namespace Manatite.Enemies
{
    /// <summary>
    /// AI equivalent of <see cref="Weapon"/>: no input, just fires forward on a
    /// <see cref="WeaponData"/>-driven cooldown while enabled and <see cref="canFire"/>.
    /// Relies on <see cref="EnemyController"/> already turning this enemy to face its
    /// target, so no separate aiming/range logic is needed for MVP.
    /// </summary>
    public sealed class EnemyWeapon : MonoBehaviour
    {
        [SerializeField] private WeaponData data;
        [Tooltip("Where projectiles spawn/aim from. Defaults to this transform if unset.")]
        [SerializeField] private Transform firePoint;
        [Tooltip("Per-enemy toggle: uncheck to disable shooting without removing the component.")]
        [SerializeField] private bool canFire = true;

        private float _cooldownRemaining;

        private void Awake()
        {
            if (firePoint == null) firePoint = transform;
            if (data == null)
                Debug.LogError($"{nameof(EnemyWeapon)} on '{name}' has no {nameof(WeaponData)} assigned.", this);
        }

        private void OnEnable() => _cooldownRemaining = 0f;

        private void Update()
        {
            if (!canFire || data == null) return;

            _cooldownRemaining -= Time.deltaTime;
            if (_cooldownRemaining <= 0f) Fire();
        }

        private void Fire()
        {
            ProjectileLauncher.Fire(data, firePoint);
            _cooldownRemaining = 1f / Mathf.Max(data.FireRate, 0.0001f);
        }
    }
}
