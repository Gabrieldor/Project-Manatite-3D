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
                float value = chip.RolledValues[i];
                Color color = value > 0f ? positiveColor : value < 0f ? negativeColor : neutralColor;
                string colorHex = ColorUtility.ToHtmlStringRGB(color);
                string label = GetStatDisplayName(modifiers[i].StatTarget);

                if (modifiers[i].ValueKind == ChipValueKind.Flat)
                {
                    string text = string.Format(CultureInfo.InvariantCulture, "{0:+0.#;-0.#;0} {1}", value, label);
                    flatLines.Add($"<color=#{colorHex}>{text}</color>");
                }
                else
                {
                    string text = string.Format(CultureInfo.InvariantCulture, "{0:+0.#;-0.#;0}% {1}", value * 100f, label);
                    percentLines.Add($"<color=#{colorHex}>{text}</color>");
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
                default: return target.ToString();
            }
        }
    }
}
