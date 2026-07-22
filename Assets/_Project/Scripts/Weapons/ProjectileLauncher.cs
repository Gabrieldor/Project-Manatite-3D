using Manatite.Combat;
using Manatite.Core;
using Manatite.Data;
using UnityEngine;

namespace Manatite.Weapons
{
    /// <summary>
    /// Shared fire path for anything shooting a <see cref="WeaponData"/>-defined
    /// projectile (player <see cref="Weapon"/>, <see cref="Manatite.Enemies.EnemyWeapon"/>).
    /// Spawns via <see cref="ObjectPooler"/> and wires the hit to <see cref="Health"/> damage.
    /// </summary>
    internal static class ProjectileLauncher
    {
        public static void Fire(WeaponData data, Transform firePoint)
        {
            GameObject instance = ObjectPooler.Get(data.ProjectilePrefab, firePoint.position, firePoint.rotation);
            Projectile projectile = instance.GetComponent<Projectile>();

            float damage = data.Damage;
            projectile.OnHit += (_, collision) => ApplyDamage(collision, damage);
            projectile.Launch(data.ProjectileSpeed, data.ProjectileLifetime);
        }

        private static void ApplyDamage(Collision collision, float damage)
        {
            Health health = collision.collider.GetComponentInParent<Health>();
            if (health != null) health.TakeDamage(damage);
        }
    }
}
