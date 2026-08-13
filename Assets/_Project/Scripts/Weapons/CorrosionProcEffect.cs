using Manatite.Combat;

namespace Manatite.Weapons
{
    /// <summary>
    /// Applies (or refreshes) the <see cref="CorrosionEffect"/> DoT status on whatever a hit
    /// lands on. Reacts to every hit generically like every other effect, so a merged
    /// Corrosion+Lightning chip spreads the status to every chain target, not just the first —
    /// and, since <see cref="CorrosionEffect"/> itself re-raises a hit on each tick, a corroded
    /// target can go on to trigger Splash/Lightning for as long as the DoT lasts.
    /// </summary>
    internal sealed class CorrosionProcEffect : IProcEffect
    {
        public void TryProc(in HitContext hit, in EffectProfile profile, int depth, ProcBus.EnqueueHit enqueue)
        {
            if (profile.CorrosionDps <= 0 || profile.CorrosionDuration <= 0f) return;
            // A dead target's Health component can still be a live (non-null) C# reference for
            // a few more instructions after it's pooled — GetComponent/AddComponent below would
            // otherwise revive corrosion on a GameObject that's already back in the pool.
            if (hit.Target == null || !hit.Target.IsAlive) return;
            if (hit.OwnerTag != null && hit.Target.CompareTag(hit.OwnerTag)) return;

            CorrosionEffect corrosion = hit.Target.GetComponent<CorrosionEffect>();
            if (corrosion == null) corrosion = hit.Target.gameObject.AddComponent<CorrosionEffect>();
            corrosion.Apply(profile, hit.OwnerTag);
        }
    }
}
