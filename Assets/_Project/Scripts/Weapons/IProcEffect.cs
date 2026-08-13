namespace Manatite.Weapons
{
    /// <summary>
    /// One equipped effect's reaction to a hit. Implementations only ever look at their own
    /// field(s) on <see cref="EffectProfile"/> — they never know or care whether any other
    /// effect is equipped, which is what lets two effects trigger each other (e.g. a lightning
    /// hop detonating an explosion) with no per-combination code: whatever damage a handler
    /// deals gets re-raised through <paramref name="enqueue"/>, and every handler — including
    /// itself — gets a look at that new hit too.
    /// </summary>
    internal interface IProcEffect
    {
        void TryProc(in HitContext hit, in EffectProfile profile, int depth, ProcBus.EnqueueHit enqueue);
    }
}
