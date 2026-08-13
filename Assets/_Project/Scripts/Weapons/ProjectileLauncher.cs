using Manatite.Audio;
using Manatite.Combat;
using Manatite.Core;
using UnityEngine;

namespace Manatite.Weapons
{
    /// <summary>
    /// Shared fire path for anything shooting a projectile (player <see cref="Weapon"/>,
    /// <see cref="Manatite.Enemies.EnemyWeapon"/>), independent of which data asset the
    /// caller sources its stats from. Spawns via <see cref="ObjectPooler"/>, deals the
    /// projectile's own base damage on impact, then hands the hit to <see cref="ProcBus"/> so
    /// every equipped effect (splash, corrosion, lightning, ...) gets a chance to react —
    /// including reacting to each other's follow-up damage.
    /// </summary>
    internal static class ProjectileLauncher
    {
        public static void Fire(GameObject projectilePrefab, float projectileSpeed, float projectileLifetime, Transform firePoint, Collider ownerCollider, in EffectProfile profile, GameObject hitMarkerPrefab = null, float projectileScale = 1f)
            => Fire(projectilePrefab, projectileSpeed, projectileLifetime, firePoint.position, firePoint.rotation, ownerCollider, profile, hitMarkerPrefab, projectileScale);

        public static void Fire(GameObject projectilePrefab, float projectileSpeed, float projectileLifetime, Vector3 position, Quaternion rotation, Collider ownerCollider, in EffectProfile profile, GameObject hitMarkerPrefab = null, float projectileScale = 1f)
        {
            GameObject instance = ObjectPooler.Get(projectilePrefab, position, rotation);
            // Read the scale from the prefab asset (not the pooled instance) so a stale scale
            // from a previous shooter (e.g. a Large enemy) can never leak onto reused instances.
            instance.transform.localScale = projectilePrefab.transform.localScale * projectileScale;
            Projectile projectile = instance.GetComponent<Projectile>();

            projectile.IgnoreOwnerCollider(ownerCollider);

            // Same-tag targets (e.g. two enemies) never damage each other, regardless of
            // aim or how close together they end up while swarming a shared target.
            string ownerTag = ownerCollider != null ? ownerCollider.tag : null;
            // `profile` is an `in` parameter, which a closure can't capture directly.
            EffectProfile capturedProfile = profile;
            GameObject capturedHitMarker = hitMarkerPrefab;
            projectile.OnHit += (_, collision) => ApplyPrimaryHit(collision, capturedProfile, ownerTag, capturedHitMarker);
            projectile.Launch(projectileSpeed, projectileLifetime);

            // The player's own gun always sounds equally present (2D); anything else firing
            // (enemies) is spatial so the player can hear where shots are coming from.
            bool shooterIsPlayer = ownerTag == GameLayers.PlayerTag;
            if (shooterIsPlayer) AudioManager.PlaySfx(SfxCue.Shot);
            else AudioManager.PlaySfx3D(SfxCue.Shot, position);
        }

        private static void ApplyPrimaryHit(Collision collision, EffectProfile profile, string ownerTag, GameObject hitMarkerPrefab)
        {
            Health primaryHealth = collision.collider.GetComponentInParent<Health>();
            Vector3 hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : collision.collider.transform.position;
            hitPoint = ClampToTargetBounds(hitPoint, primaryHealth);

            DamageHealth(primaryHealth, profile.Damage, ownerTag);

            // Feedback for the player's own shots landing, regardless of any chip effect.
            if (primaryHealth != null && ownerTag == GameLayers.PlayerTag)
                SpawnHitMarker(hitMarkerPrefab, hitPoint);

            ProcBus.RaiseHit(new HitContext(hitPoint, primaryHealth, ownerTag), profile);
        }

        // The hit collider can be larger than the visible mesh (e.g. enemy hulls use an
        // oversized hitbox), which places the raw contact point outside the model. Clamping it
        // onto the target's actual render bounds keeps hit-marker/splash/lightning VFX on the
        // ship instead of floating in front of it. Shared by every IProcEffect handler.
        internal static Vector3 ClampToTargetBounds(Vector3 point, Health target)
        {
            if (target == null) return point;

            Renderer targetRenderer = target.GetComponentInChildren<Renderer>();
            return targetRenderer != null ? targetRenderer.bounds.ClosestPoint(point) : point;
        }

        // Shared by every IProcEffect handler so hit-taken/hit-dealt feedback stays consistent
        // regardless of which effect caused the damage.
        internal static void DamageHealth(Health health, int damage, string ownerTag)
        {
            if (health == null) return;
            if (ownerTag != null && health.CompareTag(ownerTag)) return;

            health.TakeDamage(damage);

            // Both are 2D hit-marker-style feedback, not ambient world sound, so they stay
            // equally audible regardless of where the hit landed.
            if (health.CompareTag(GameLayers.PlayerTag)) AudioManager.PlaySfx(SfxCue.HitTaken);
            else if (ownerTag == GameLayers.PlayerTag) AudioManager.PlaySfx(SfxCue.HitDealt);
        }

        private static void SpawnHitMarker(GameObject hitMarkerPrefab, Vector3 position)
        {
            if (hitMarkerPrefab == null) return;

            GameObject instance = ObjectPooler.Get(hitMarkerPrefab, position, Quaternion.identity);
            if (instance.TryGetComponent(out HitMarkerEffect marker))
                marker.Play();
        }
    }
}
