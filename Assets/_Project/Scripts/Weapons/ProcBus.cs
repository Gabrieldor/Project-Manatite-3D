using System.Collections.Generic;

namespace Manatite.Weapons
{
    /// <summary>
    /// Fans a hit out through every equipped effect (<see cref="IProcEffect"/>), and every
    /// further hit those effects cause, without any of them knowing about each other. This is
    /// the generic cross-triggering seam: merging or simply equipping Explosion and Lightning
    /// together needs no combination-specific code, because a lightning hop's damage re-enters
    /// this same bus and the Splash handler reacts to it exactly like it would a fresh
    /// projectile impact. Each queued hit remembers which effect produced it so that effect is
    /// skipped when its own hit comes back around — a proc never re-triggers itself, only other
    /// effects react to it — and bounded by <see cref="MaxDepth"/>/<see cref="MaxTotalHits"/> so
    /// a dense cluster of targets can't runaway the cost of a single shot or corrosion tick.
    /// </summary>
    internal static class ProcBus
    {
        public delegate void EnqueueHit(HitContext hit, int depth);

        private const int MaxDepth = 3;
        // Cross-effect chain reactions (e.g. an explosion's blast triggering a lightning hop
        // that detonates another explosion) are an intentional feature, not a bug — this just
        // keeps a dense cluster from turning one shot into a long fireworks show.
        private const int MaxTotalHits = 4;
        private const int NoProducer = -1;

        private static readonly IProcEffect[] Effects =
        {
            new SplashProcEffect(),
            new LightningProcEffect(),
            new CorrosionProcEffect(),
        };

        // One enqueue delegate per effect, built once — each closes over which effect is doing
        // the enqueueing so EnqueueInternal can tag the entry with its producer.
        private static readonly EnqueueHit[] EnqueueByProducer = BuildEnqueueDelegates();

        // Reused across calls instead of allocated per hit — safe because Unity is
        // single-threaded and each RaiseHit call runs to completion before another can start.
        private static readonly List<(HitContext Hit, int Depth, int ProducerIndex)> Pending = new List<(HitContext, int, int)>(MaxTotalHits);

        private static EnqueueHit[] BuildEnqueueDelegates()
        {
            var delegates = new EnqueueHit[Effects.Length];
            for (int i = 0; i < delegates.Length; i++)
            {
                int producerIndex = i; // local copy — capturing the loop variable itself would let every delegate see the final value
                delegates[i] = (hit, depth) => EnqueueInternal(hit, depth, producerIndex);
            }
            return delegates;
        }

        /// <summary>Raises a fresh hit at depth 0 — used both for a shot's initial impact and
        /// for a deferred event (e.g. a corrosion tick) that isn't part of any prior chain.</summary>
        public static void RaiseHit(HitContext hit, in EffectProfile profile)
        {
            Pending.Clear();
            Pending.Add((hit, 0, NoProducer));

            for (int i = 0; i < Pending.Count; i++)
            {
                (HitContext currentHit, int depth, int producerIndex) = Pending[i];
                for (int e = 0; e < Effects.Length; e++)
                {
                    if (e == producerIndex) continue;
                    Effects[e].TryProc(currentHit, profile, depth, EnqueueByProducer[e]);
                }
            }
        }

        private static void EnqueueInternal(HitContext hit, int depth, int producerIndex)
        {
            if (depth <= MaxDepth && Pending.Count < MaxTotalHits)
                Pending.Add((hit, depth, producerIndex));
        }
    }
}
