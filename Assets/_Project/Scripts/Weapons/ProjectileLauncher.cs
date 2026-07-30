using Manatite.Combat;
using Manatite.Core;
using UnityEngine;

namespace Manatite.Weapons
{
    /// <summary>
    /// Shared fire path for anything shooting a projectile (player <see cref="Weapon"/>,
    /// <see cref="Manatite.Enemies.EnemyWeapon"/>), independent of which data asset the
    /// caller sources its stats from. Spawns via <see cref="ObjectPooler"/> and wires the
    /// hit to <see cref="Health"/> damage.
    /// </summary>
    internal static class ProjectileLauncher
    {
        public static void Fire(GameObject projectilePrefab, int damage, float projectileSpeed, float projectileLifetime, Transform firePoint, Collider ownerCollider = null)
        {
            GameObject instance = ObjectPooler.Get(projectilePrefab, firePoint.position, firePoint.rotation);
            Projectile projectile = instance.GetComponent<Projectile>();

            projectile.IgnoreOwnerCollider(ownerCollider);

            // Same-tag targets (e.g. two enemies) never damage each other, regardless of
            // aim or how close together they end up while swarming a shared target.
            string ownerTag = ownerCollider != null ? ownerCollider.tag : null;
            projectile.OnHit += (_, collision) => ApplyDamage(collision, damage, ownerTag);
            projectile.Launch(projectileSpeed, projectileLifetime);
        }

        private static void ApplyDamage(Collision collision, int damage, string ownerTag)
        {
            Health health = collision.collider.GetComponentInParent<Health>();
            if (health == null) return;
            if (ownerTag != null && health.CompareTag(ownerTag)) return;

            health.TakeDamage(damage);
        }
    }
}
