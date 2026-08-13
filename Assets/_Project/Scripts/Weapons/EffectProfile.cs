using Manatite.Data;
using Manatite.Player;
using UnityEngine;

namespace Manatite.Weapons
{
    /// <summary>
    /// Every chip-effect magnitude and VFX prefab a shot resolves with, captured once — either
    /// when the shot fires or when a status effect is applied — so downstream reactions
    /// (including a <see cref="Manatite.Combat.CorrosionEffect"/> tick seconds later) use the
    /// values equipped at that moment rather than a live, possibly-stale lookup. Replaces what
    /// used to be a growing list of loose parameters threaded through
    /// <see cref="Weapon"/> → <see cref="ProjectileLauncher"/> → each effect: adding a new
    /// effect now only means adding a field here and a handler in <see cref="ProcBus"/>, not
    /// touching every signature in between.
    /// </summary>
    public readonly struct EffectProfile
    {
        public readonly int Damage;
        public readonly float SplashRadius;
        public readonly GameObject SplashVfxPrefab;
        public readonly float CorrosionDuration;
        public readonly int CorrosionDps;
        public readonly float LightningRange;
        public readonly int LightningChainCount;
        public readonly int LightningDamage;
        public readonly GameObject LightningVfxPrefab;

        public EffectProfile(int damage, float splashRadius, GameObject splashVfxPrefab, float corrosionDuration, int corrosionDps, float lightningRange, int lightningChainCount, int lightningDamage, GameObject lightningVfxPrefab)
        {
            Damage = damage;
            SplashRadius = splashRadius;
            SplashVfxPrefab = splashVfxPrefab;
            CorrosionDuration = corrosionDuration;
            CorrosionDps = corrosionDps;
            LightningRange = lightningRange;
            LightningChainCount = lightningChainCount;
            LightningDamage = lightningDamage;
            LightningVfxPrefab = lightningVfxPrefab;
        }

        /// <summary>No chip effects at all — just a base damage number. Used by shooters with no
        /// <see cref="PlayerInventory"/> (currently every <see cref="Manatite.Enemies.EnemyWeapon"/>).</summary>
        public static EffectProfile BaseDamageOnly(int damage) => new EffectProfile(damage, 0f, null, 0f, 0, 0f, 0, 0, null);

        /// <summary>Aggregates every equipped chip's contribution to each effect (same
        /// flat-sum/multiplier math <see cref="PlayerInventory.GetModifiedFloat"/> already does)
        /// into one snapshot for this shot.</summary>
        public static EffectProfile FromInventory(PlayerInventory inventory, int baseDamage, GameObject splashVfxPrefab, GameObject lightningVfxPrefab)
        {
            if (inventory == null) return new EffectProfile(baseDamage, 0f, splashVfxPrefab, 0f, 0, 0f, 0, 0, lightningVfxPrefab);

            return new EffectProfile(
                inventory.GetModifiedInt(ChipStatTarget.Damage, baseDamage),
                inventory.GetModifiedFloat(ChipStatTarget.SplashRadius, 0f), splashVfxPrefab,
                inventory.GetModifiedFloat(ChipStatTarget.CorrosionDuration, 0f),
                inventory.GetModifiedInt(ChipStatTarget.CorrosionDps, 0),
                inventory.GetModifiedFloat(ChipStatTarget.LightningRange, 0f),
                inventory.GetModifiedInt(ChipStatTarget.LightningChainCount, 0),
                inventory.GetModifiedInt(ChipStatTarget.LightningDamage, 0),
                lightningVfxPrefab);
        }
    }
}
