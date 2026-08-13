using System.Collections.Generic;
using Manatite.Audio;
using Manatite.Combat;
using Manatite.Core;
using UnityEngine;

namespace Manatite.Weapons
{
    /// <summary>
    /// Bounces from the hit point through up to <see cref="EffectProfile.LightningChainCount"/>
    /// other targets. Each hop always prefers a target not yet struck by this chain, even if a
    /// farther one is unstruck and a closer one was already hit — only re-strikes an
    /// already-hit target when nothing unstruck remains in range, so the chain keeps going in
    /// sparse fights instead of fizzling out early.
    /// </summary>
    internal sealed class LightningProcEffect : IProcEffect
    {
        private static readonly Collider[] ColliderBuffer = new Collider[32];
        private static readonly HashSet<Health> StruckThisChain = new HashSet<Health>();

        public void TryProc(in HitContext hit, in EffectProfile profile, int depth, ProcBus.EnqueueHit enqueue)
        {
            if (profile.LightningChainCount <= 0 || profile.LightningRange <= 0f) return;

            StruckThisChain.Clear();
            if (hit.Target != null) StruckThisChain.Add(hit.Target);

            Vector3 currentPoint = hit.Point;
            Health currentHealth = hit.Target;

            for (int i = 0; i < profile.LightningChainCount; i++)
            {
                Health target = FindNearestChainTarget(currentPoint, profile.LightningRange, hit.OwnerTag, currentHealth, StruckThisChain);
                if (target == null) break;

                Vector3 targetPoint = ProjectileLauncher.ClampToTargetBounds(target.transform.position, target);
                SpawnLightningArc(profile.LightningVfxPrefab, currentPoint, targetPoint);
                ProjectileLauncher.DamageHealth(target, profile.LightningDamage, hit.OwnerTag);
                StruckThisChain.Add(target);

                enqueue(new HitContext(targetPoint, target, hit.OwnerTag), depth + 1);

                currentPoint = targetPoint;
                currentHealth = target;
            }
        }

        private static Health FindNearestChainTarget(Vector3 origin, float range, string ownerTag, Health excludeSelf, HashSet<Health> struckThisChain)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, range, ColliderBuffer);
            Health best = null;
            bool bestUnstruck = false;
            float bestSqrDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Health candidate = ColliderBuffer[i].GetComponentInParent<Health>();
                if (candidate == null || candidate == excludeSelf) continue;
                if (ownerTag != null && candidate.CompareTag(ownerTag)) continue;

                bool unstruck = !struckThisChain.Contains(candidate);
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
            AudioManager.PlaySfx3D(SfxCue.Lightning, Vector3.Lerp(from, to, 0.5f));

            if (lightningVfxPrefab == null) return;

            GameObject instance = ObjectPooler.Get(lightningVfxPrefab, from, Quaternion.identity);
            if (instance.TryGetComponent(out LightningArcEffect arc))
                arc.Play(from, to);
        }
    }
}
