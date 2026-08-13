using System;
using UnityEngine;

namespace Manatite.Data
{
    /// <summary>
    /// One stat effect within a <see cref="ChipData"/>. A chip can carry more than one of
    /// these (e.g. the Splash Damage chip modifies fire rate, damage, and splash radius at once).
    /// </summary>
    [Serializable]
    public struct ChipModifier
    {
        private const float FlatMin = -5f;
        private const float FlatMax = 5f;
        private const float MultiplierMin = -0.75f;
        private const float MultiplierMax = 1f;
        // World-space distance, not a small stat delta — enemies alone measure 20-170 units
        // across, so this needs its own envelope rather than the Flat range above.
        private const float SplashRadiusMin = 0f;
        private const float SplashRadiusMax = 150f;
        // Seconds a corrosion status lingers, and its int damage-per-second — neither is a
        // stat delta against a baseline like Flat/Multiplier, so each gets its own envelope.
        private const float CorrosionDurationMin = 0f;
        private const float CorrosionDurationMax = 10f;
        private const float CorrosionDpsMin = 0f;
        private const float CorrosionDpsMax = 5f;
        // World-space chain-search radius, chain hop count, and per-hop damage — same reasoning
        // as SplashRadius/Corrosion above, each is its own standalone stat, not a base delta.
        private const float LightningRangeMin = 0f;
        private const float LightningRangeMax = 1000f;
        private const float LightningChainCountMin = 0f;
        private const float LightningChainCountMax = 15f;
        private const float LightningDamageMin = 0f;
        private const float LightningDamageMax = 15f;

        [SerializeField] private ChipStatTarget statTarget;
        [SerializeField] private ChipValueKind valueKind;
        [Tooltip("Rolled value range for a new instance of this modifier. Flat: -5..+5. " +
                 "Multiplier: -75%..+100% (stored as -0.75..1.0). SplashRadius: 0..150 (world units). " +
                 "LightningRange: 0..1000 (world units). LightningChainCount: 0..15. LightningDamage: 0..15.")]
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue;

        public ChipStatTarget StatTarget => statTarget;
        public ChipValueKind ValueKind => valueKind;
        public float MinValue => minValue;
        public float MaxValue => maxValue;

        /// <summary>Used by <see cref="Manatite.Core.ChipMerger"/> to build a merged chip's rows
        /// from an already-computed value rather than a min/max roll range.</summary>
        public ChipModifier(ChipStatTarget statTarget, ChipValueKind valueKind, float minValue, float maxValue)
        {
            this.statTarget = statTarget;
            this.valueKind = valueKind;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }

        public void ClampToValueKindRange()
        {
            float lo, hi;
            if (statTarget == ChipStatTarget.SplashRadius)
            {
                lo = SplashRadiusMin;
                hi = SplashRadiusMax;
            }
            else if (statTarget == ChipStatTarget.CorrosionDuration)
            {
                lo = CorrosionDurationMin;
                hi = CorrosionDurationMax;
            }
            else if (statTarget == ChipStatTarget.CorrosionDps)
            {
                lo = CorrosionDpsMin;
                hi = CorrosionDpsMax;
            }
            else if (statTarget == ChipStatTarget.LightningRange)
            {
                lo = LightningRangeMin;
                hi = LightningRangeMax;
            }
            else if (statTarget == ChipStatTarget.LightningChainCount)
            {
                lo = LightningChainCountMin;
                hi = LightningChainCountMax;
            }
            else if (statTarget == ChipStatTarget.LightningDamage)
            {
                lo = LightningDamageMin;
                hi = LightningDamageMax;
            }
            else
            {
                lo = valueKind == ChipValueKind.Flat ? FlatMin : MultiplierMin;
                hi = valueKind == ChipValueKind.Flat ? FlatMax : MultiplierMax;
            }

            minValue = Mathf.Clamp(minValue, lo, hi);
            maxValue = Mathf.Clamp(maxValue, lo, hi);
        }
    }
}
