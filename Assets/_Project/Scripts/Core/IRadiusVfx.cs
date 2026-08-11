namespace Manatite.Core
{
    /// <summary>
    /// Common contract for pooled, radius-scaled VFX (e.g. splash-damage explosions) so
    /// <c>WeaponData.splashVfxPrefab</c> can be swapped between implementations
    /// (<see cref="FadingSphereEffect"/>, <see cref="ParticleExplosionEffect"/>) without the
    /// spawning code needing to know which one is assigned.
    /// </summary>
    public interface IRadiusVfx
    {
        void Play(float radius);
    }
}
