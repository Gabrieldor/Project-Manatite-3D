namespace Manatite.Audio
{
    /// <summary>
    /// Identifies one in-flight <see cref="AudioManager.PlayLoopingSfx"/> instance so it can
    /// later be stopped with <see cref="AudioManager.StopLoopingSfx"/>. <see cref="Invalid"/>
    /// (or any handle from a call that found nothing to play) is safe to pass to Stop — it's
    /// simply a no-op.
    /// </summary>
    public readonly struct LoopingSfxHandle
    {
        public static readonly LoopingSfxHandle Invalid = new LoopingSfxHandle(-1, -1);

        public int VoiceIndex { get; }
        public int Generation { get; }
        public bool IsValid => VoiceIndex >= 0;

        internal LoopingSfxHandle(int voiceIndex, int generation)
        {
            VoiceIndex = voiceIndex;
            Generation = generation;
        }
    }
}
