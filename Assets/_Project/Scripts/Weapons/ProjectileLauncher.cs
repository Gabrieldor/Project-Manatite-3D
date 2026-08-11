using System.Collections.Generic;
using Manatite.Audio;
using Manatite.Combat;
using Manatite.Core;
using UnityEngine;

namespace Manatite.Weapons
{
    /// <summary>
    /// Shared fire path for anything shooting a projectile (player <see cref="Weapon"/>,
    /// <see cref="Manatite.Enemies.EnemyWeapon"/>), independent of which data asset the
    /// caller sources its stats from. Spawns via <see cref="ObjectPooler"/> and wires the
    /// hit to <see cref="Health"/> damage, optionally splashing to nearby targets.
    /// </summary>
    internal static class ProjectileLauncher
    {
        private static readonly Collider[] SplashColliderBuffer = new Collider[32];
        private static readonly Collider[] LightningColliderBuffer = new Collider[32];

        public static void Fire(GameObject projectilePrefab, int damage, float projectileSpeed, float projectileLifetime, Transform firePoint, Collider ownerCollider = null, float splashRadius = 0f, GameObject splashVfxPrefab = null, GameObject hitMarkerPrefab = null, float corrosionDuration = 0f, int corrosionDps = 0, float lightningRange = 0f, int lightningChainCount = 0, int lightningDamage = 0, GameObject lightningVfxPrefab = null)
            => Fire(projectilePrefab, damage, projectileSpeed, projectileLifetime, firePoint.position, firePoint.rotation, ownerCollider, splashRadius, splashVfxPrefab, hitMarkerPrefab, corrosionDuration, corrosionDps, lightningRange, lightningChainCount, lightningDamage, lightningVfxPrefab);

        public static void Fire(GameObject projectilePrefab, int damage, float projectileSpeed, float projectileLifetime, Vector3 position, Quaternion rotation, Collider ownerCollider = null, float splashRadius = 0f, GameObject splashVfxPrefab = null, GameObject hitMarkerPrefab = null, float corrosionDuration = 0f, int corrosionDps = 0, float lightningRange = 0f, int lightningChainCount = 0, int lightningDamage = 0, GameObject lightningVfxPrefab = null)
        {
            GameObject instance = ObjectPooler.Get(projectilePrefab, position, rotation);
            Projectile projectile = instance.GetComponent<Projectile>();

            projectile.IgnoreOwnerCollider(ownerCollider);

            // Same-tag targets (e.g. two enemies) never damage each other, regardless of
            // aim or how close together they end up while swarming a shared target.
            string ownerTag = ownerCollider != null ? ownerCollider.tag : null;
            projectile.OnHit += (_, collision) => ApplyDamage(collision, damage, ownerTag, splashRadius, splashVfxPrefab, hitMarkerPrefab, corrosionDuration, corrosionDps, lightningRange, lightningChainCount, lightningDamage, lightningVfxPrefab);
            projectile.Launch(projectileSpeed, projectileLifetime);

            // The player's own gun always sounds equally present (2D); anything else firing
            // (enemies) is spatial so the player can hear where shots are coming from.
            bool shooterIsPlayer = ownerTag == GameLayers.PlayerTag;
            if (shooterIsPlayer) AudioManager.PlaySfx(SfxCue.Shot);
            else AudioManager.PlaySfx3D(SfxCue.Shot, position);
        }

        private static void ApplyDamage(Collision collision, int damage, string ownerTag, float splashRadius, GameObject splashVfxPrefab, GameObject hitMarkerPrefab, float corrosionDuration, int corrosionDps, float lightningRange, int lightningChainCount, int lightningDamage, GameObject lightningVfxPrefab)
        {
            Health primaryHealth = collision.collider.GetComponentInParent<Health>();
            DamageHealth(primaryHealth, damage, ownerTag);

            Vector3 hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : collision.collider.transform.position;
            hitPoint = ClampToTargetBounds(hitPoint, primaryHealth);

            // Feedback for the player's own shots landing, regardless of any chip effect.
            if (primaryHealth != null && ownerTag == GameLayers.PlayerTag)
                SpawnHitMarker(hitMarkerPrefab, hitPoint);

            if (primaryHealth != null && corrosionDuration > 0f && corrosionDps > 0
                && (ownerTag == null || !primaryHealth.CompareTag(ownerTag)))
            {
                CorrosionEffect corrosion = primaryHealth.GetComponent<CorrosionEffect>();
                if (corrosion == null) corrosion = primaryHealth.gameObject.AddComponent<CorrosionEffect>();
                corrosion.Apply(corrosionDps, corrosionDuration);
            }

            if (splashRadius > 0f)
            {
                ApplySplashDamage(hitPoint, splashRadius, damage, ownerTag, primaryHealth);
                SpawnSplashVfx(splashVfxPrefab, hitPoint, splashRadius);
            }

            if (lightningChainCount > 0 && lightningRange > 0f)
                ChainLightning(hitPoint, primaryHealth, lightningRange, lightningChainCount, lightningDamage, ownerTag, lightningVfxPrefab);
        }

        // Bounces from the hit point through up to chainCount other targets. Each hop always
        // prefers a target not yet struck by this shot, even if a farther one is unstruck and a
        // closer one was already hit — only re-strikes an already-hit target when nothing
        // unstruck remains in range, so the chain keeps going in sparse fights instead of
        // fizzling out early. The node the bolt is currently at is always excluded (no
        // zero-distance bounce straight back to where it just came from).
        private static void ChainLightning(Vector3 originPoint, Health primaryHealth, float range, int chainCount, int damage, string ownerTag, GameObject lightningVfxPrefab)
        {
            var struckThisShot = new HashSet<Health>();
            if (primaryHealth != null) struckThisShot.Add(primaryHealth);

            Vector3 currentPoint = originPoint;
            Health currentHealth = primaryHealth;

            for (int i = 0; i < chainCount; i++)
            {
                Health target = FindNearestChainTarget(currentPoint, range, ownerTag, currentHealth, struckThisShot);
                if (target == null) break;

                Vector3 targetPoint = ClampToTargetBounds(target.transform.position, target);
                SpawnLightningArc(lightningVfxPrefab, currentPoint, targetPoint);
                DamageHealth(target, damage, ownerTag);
                struckThisShot.Add(target);

                currentPoint = targetPoint;
                currentHealth = target;
            }
        }

        private static Health FindNearestChainTarget(Vector3 origin, float range, string ownerTag, Health excludeSelf, HashSet<Health> struckThisShot)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, range, LightningColliderBuffer);
            Health best = null;
            bool bestUnstruck = false;
            float bestSqrDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Health candidate = LightningColliderBuffer[i].GetComponentInParent<Health>();
                if (candidate == null || candidate == excludeSelf) continue;
                if (ownerTag != null && candidate.CompareTag(ownerTag)) continue;

                bool unstruck = !struckThisShot.Contains(candidate);
                float sqrDistance = (candidate.transform.position - origin).sqrMagnitude;

                if (best == null || (unstruck && !bestUnstruck) || (unstruck == bestUnstruck && sqrDistance < bestSqrDistance))
                {
                    best = candidate;
                    bestUnstruck = unstruck;
                    bestSqrDistance = sqrDistance;
                }
            }

            return best;
        }

        private static void SpawnLightningArc(GameObject lightningVfxPrefab, Vector3 from, Vector3 to)
        {
            if (lightningVfxPrefab == null) return;

            GameObject instance = ObjectPooler.Get(lightningVfxPrefab, from, Quaternion.identity);
            if (instance.TryGetComponent(out LightningArcEffect arc))
                arc.Play(from, to);
        }

        // The hit collider can be larger than the visible mesh (e.g. enemy hulls use an
        // oversized hitbox), which places the raw contact point outside the model. Clamping it
        // onto the target's actual render bounds keeps hit-marker/splash VFX on the ship
        // instead of floating in front of it.
        private static Vector3 ClampToTargetBounds(Vector3 point, Health target)
        {
            if (target == null) return point;

            Renderer targetRenderer = target.GetComponentInChildren<Renderer>();
            return targetRenderer != null ? targetRenderer.bounds.ClosestPoint(point) : point;
        }

        private static void ApplySplashDamage(Vector3 origin, float radius, int damage, string ownerTag, Health excludeHealth)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, radius, SplashColliderBuffer);
            var alreadyDamaged = new HashSet<Health>();
            for (int i = 0; i < count; i++)
            {
                Health health = SplashColliderBuffer[i].GetComponentInParent<Health>();
                if (health == null || health == excludeHealth || !alreadyDamaged.Add(health)) continue;

                DamageHealth(health, damage, ownerTag);
            }
        }

        private static void DamageHealth(Health health, int damage, string ownerTag)
        {
            if (health == null) return;
            if (ownerTag != null && health.CompareTag(ownerTag)) return;

            health.TakeDamage(damage);

            // Both are 2D hit-marker-style feedback, not ambient world sound, so they stay
            // equally audible regardless of where the hit landed.
            if (health.CompareTag(GameLayers.PlayerTag)) AudioManager.PlaySfx(SfxCue.HitTaken);
            else if (ownerTag == GameLayers.PlayerTag) AudioManager.PlaySfx(SfxCue.HitDealt);
        }

        private static void SpawnSplashVfx(GameObject splashVfxPrefab, Vector3 position, float radius)
        {
            if (splashVfxPrefab == null) return;

            GameObject instance = ObjectPooler.Get(splashVfxPrefab, position, Quaternion.identity);
            if (instance.TryGetComponent(out FadingSphereEffect effect))
                effect.Play(radius);
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
