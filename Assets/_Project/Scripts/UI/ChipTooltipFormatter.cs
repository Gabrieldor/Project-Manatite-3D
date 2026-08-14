using System.Collections.Generic;
using System.Globalization;
using Manatite.Core;
using Manatite.Data;
using UnityEngine;

namespace Manatite.UI
{
    /// <summary>
    /// Builds the colored flat/percent modifier line text shared by any UI that displays a
    /// <see cref="ChipInstance"/>'s stat breakdown (inventory tooltip, loot notification).
    /// </summary>
    public static class ChipTooltipFormatter
    {
        public static void BuildStatLines(ChipInstance chip, Color positiveColor, Color negativeColor, Color neutralColor, out string flatText, out string percentText)
        {
            ChipModifier[] modifiers = chip.Data.Modifiers;
            var flatLines = new List<string>();
            var percentLines = new List<string>();

            for (int i = 0; i < modifiers.Length; i++)
            {
                if (modifiers[i].StatTarget == ChipStatTarget.SplashRadius) continue;

                float value = chip.RolledValues[i];
                Color color = value > 0f ? positiveColor : value < 0f ? negativeColor : neutralColor;
                string colorHex = ColorUtility.ToHtmlStringRGB(color);
                string label = GetStatDisplayName(modifiers[i].StatTarget);

                // ReflectChance is stored as a raw 0..1 probability via the Flat pool (see
                // ChipModifier), but reads like a percent stat to the player.
                bool displayAsPercent = modifiers[i].ValueKind == ChipValueKind.Multiplier
                    || modifiers[i].StatTarget == ChipStatTarget.ReflectChance;

                if (displayAsPercent)
                {
                    string text = string.Format(CultureInfo.InvariantCulture, "{0:+0.#;-0.#;0}% {1}", value * 100f, label);
                    percentLines.Add($"<color=#{colorHex}>{text}</color>");
                }
                else
                {
                    string text = string.Format(CultureInfo.InvariantCulture, "{0:+0.#;-0.#;0} {1}", value, label);
                    flatLines.Add($"<color=#{colorHex}>{text}</color>");
                }
            }

            flatText = string.Join("\n", flatLines);
            percentText = string.Join("\n", percentLines);
        }

        public static string GetStatDisplayName(ChipStatTarget target)
        {
            switch (target)
            {
                case ChipStatTarget.FireRate: return "Weapon Fire Rate";
                case ChipStatTarget.Damage: return "Weapon Damage";
                case ChipStatTarget.SplashRadius: return "Splash Radius";
                case ChipStatTarget.CorrosionDuration: return "Corrosion Duration";
                case ChipStatTarget.CorrosionDps: return "Corrosion DPS";
                case ChipStatTarget.LightningRange: return "Lightning Range";
                case ChipStatTarget.LightningChainCount: return "Lightning Chain Count";
                case ChipStatTarget.LightningDamage: return "Lightning Damage";
                case ChipStatTarget.MoveSpeed: return "Move Speed";
                case ChipStatTarget.MaxHealth: return "Max Health";
                case ChipStatTarget.ReflectChance: return "Reflect Chance";
                case ChipStatTarget.HealAmount: return "Repair Amount";
                default: return target.ToString();
            }
        }
    }
}
