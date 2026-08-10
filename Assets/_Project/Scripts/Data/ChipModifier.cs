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

        [SerializeField] private ChipStatTarget statTarget;
        [SerializeField] private ChipValueKind valueKind;
        [Tooltip("Rolled value range for a new instance of this modifier. Flat: -5..+5. " +
                 "Multiplier: -75%..+100% (stored as -0.75..1.0). SplashRadius: 0..150 (world units).")]
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue;

        public ChipStatTarget StatTarget => statTarget;
        public ChipValueKind ValueKind => valueKind;
        public float MinValue => minValue;
        public float MaxValue => maxValue;

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
