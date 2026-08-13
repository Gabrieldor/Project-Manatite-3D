using System.Collections.Generic;
using Manatite.Audio;
using Manatite.Combat;
using Manatite.Core;
using UnityEngine;

namespace Manatite.Weapons
{
    /// <summary>Damages everything but the hit target within <see cref="EffectProfile.SplashRadius"/>.</summary>
    internal sealed class SplashProcEffect : IProcEffect
    {
        private static readonly Collider[] ColliderBuffer = new Collider[32];
        private static readonly HashSet<Health> AlreadyDamaged = new HashSet<Health>();

        public void TryProc(in HitContext hit, in EffectProfile profile, int depth, ProcBus.EnqueueHit enqueue)
        {
            if (profile.SplashRadius <= 0f) return;

            int count = Physics.OverlapSphereNonAlloc(hit.Point, profile.SplashRadius, ColliderBuffer);
            AlreadyDamaged.Clear();
            for (int i = 0; i < count; i++)
            {
                Health health = ColliderBuffer[i].GetComponentInParent<Health>();
                if (health == null || health == hit.Target || !AlreadyDamaged.Add(health)) continue;

                ProjectileLauncher.DamageHealth(health, profile.Damage, hit.OwnerTag);
                Vector3 point = ProjectileLauncher.ClampToTargetBounds(health.transform.position, health);
                enqueue(new HitContext(point, health, hit.OwnerTag), depth + 1);
            }

            SpawnSplashVfx(profile.SplashVfxPrefab, hit.Point, profile.SplashRadius);
        }

        private static void SpawnSplashVfx(GameObject splashVfxPrefab, Vector3 position, float radius)
        {
            AudioManager.PlaySfx3D(SfxCue.Explosion, position);

            if (splashVfxPrefab == null) return;

            GameObject instance = ObjectPooler.Get(splashVfxPrefab, position, Quaternion.identity);
            if (instance.TryGetComponent(out IRadiusVfx effect))
                effect.Play(radius);
        }
    }
}
