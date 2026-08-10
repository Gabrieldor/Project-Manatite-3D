using Manatite.Data;
using UnityEngine;

namespace Manatite.Core
{
    /// <summary>
    /// A rolled copy of a <see cref="ChipData"/>. Each instance gets its own random value per
    /// modifier at creation instead of mutating the shared ScriptableObject asset.
    /// </summary>
    public sealed class ChipInstance
    {
        public ChipData Data { get; }

        /// <summary>Rolled value per entry in <see cref="ChipData.Modifiers"/>, same index order.</summary>
        public float[] RolledValues { get; }

        public ChipInstance(ChipData data)
        {
            Data = data;
            ChipModifier[] modifiers = data.Modifiers;
            RolledValues = new float[modifiers.Length];
            for (int i = 0; i < modifiers.Length; i++)
                RolledValues[i] = Random.Range(modifiers[i].MinValue, modifiers[i].MaxValue);
        }
    }
}
