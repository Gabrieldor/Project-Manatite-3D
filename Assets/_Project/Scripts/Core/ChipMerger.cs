using System.Collections.Generic;
using Manatite.Data;
using UnityEngine;

namespace Manatite.Core
{
    /// <summary>
    /// Fuses two chips into one. Rows targeting the same stat combine (flat values sum;
    /// percentages compound via a diminishing-returns curve that's always an improvement over
    /// either parent — bonuses grow further from 0, penalties shrink toward 0); rows unique to
    /// one chip just carry over untouched. The result is an in-memory-only <see cref="ChipData"/>
    /// via <see cref="ChipData.CreateMerged"/>, so merging never writes a new .asset.
    /// </summary>
    public static class ChipMerger
    {
        public static ChipInstance Merge(ChipInstance a, ChipInstance b)
        {
            var rows = new List<(ChipModifier Modifier, float Value)>();
            AddOrCombine(rows, a);
            AddOrCombine(rows, b);

            var modifiers = new ChipModifier[rows.Count];
            var rolledValues = new float[rows.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                modifiers[i] = rows[i].Modifier;
                rolledValues[i] = rows[i].Value;
            }

            string name = $"{a.Data.DisplayName} + {b.Data.DisplayName}";
            string description = $"{a.Data.Description}\n{b.Data.Description}".Trim();
            // Placeholder until real fused-chip art exists — reuses the first chip's icon
            // rather than inventing new art.
            Sprite icon = a.Data.Icon;
            // Traces through any depth of re-merging back to the real chip that donated the
            // icon, so the save system can restore it by asset id instead of a raw reference.
            string iconSourceId = a.Data.IsMerged ? a.Data.IconSourceId : a.Data.name;

            ChipData merged = ChipData.CreateMerged(name, icon, modifiers, description, iconSourceId);
            return new ChipInstance(merged, rolledValues);
        }

        private static void AddOrCombine(List<(ChipModifier Modifier, float Value)> rows, ChipInstance chip)
        {
            ChipModifier[] modifiers = chip.Data.Modifiers;
            for (int i = 0; i < modifiers.Length; i++)
            {
                ChipModifier modifier = modifiers[i];
                float value = chip.RolledValues[i];

                int existingIndex = FindIndex(rows, modifier.StatTarget, modifier.ValueKind);
                if (existingIndex < 0)
                {
                    rows.Add((modifier, value));
                    continue;
                }

                float combined = Combine(modifier.ValueKind, rows[existingIndex].Value, value);
                rows[existingIndex] = (new ChipModifier(modifier.StatTarget, modifier.ValueKind, combined, combined), combined);
            }
        }

        private static int FindIndex(List<(ChipModifier Modifier, float Value)> rows, ChipStatTarget target, ChipValueKind kind)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Modifier.StatTarget == target && rows[i].Modifier.ValueKind == kind) return i;
            }
            return -1;
        }

        private static float Combine(ChipValueKind kind, float a, float b)
        {
            if (kind == ChipValueKind.Flat) return a + b;

            // Multiplier, same stat: same-sign values compound via diminishing returns (always
            // better than either parent alone). Opposite signs on the same stat just sum
            // algebraically — that's a net effect, not a "stack".
            if (a >= 0f && b >= 0f) return 1f - (1f - a) * (1f - b);
            if (a < 0f && b < 0f) return -(Mathf.Abs(a) * Mathf.Abs(b));
            return a + b;
        }
    }
}
