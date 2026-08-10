using System.Collections.Generic;
using System.Globalization;
using Manatite.Core;
using Manatite.Data;
using TMPro;
using UnityEngine;

namespace Manatite.UI
{
    public sealed class TooltipView : MonoBehaviour
    {
        [SerializeField] private InventoryCursor cursor;
        [Tooltip("Left half of the top 1/3: one line per Flat modifier on the hovered chip.")]
        [SerializeField] private TextMeshProUGUI flatStatText;
        [Tooltip("Right half of the top 1/3: one line per Multiplier modifier on the hovered chip.")]
        [SerializeField] private TextMeshProUGUI percentStatText;
        [Tooltip("Bottom 2/3: ChipData.Description verbatim.")]
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Color positiveColor = Color.green;
        [SerializeField] private Color negativeColor = Color.red;
        [SerializeField] private Color neutralColor = Color.white;

        private void OnEnable()
        {
            if (cursor == null) return;
            cursor.OnHoverChipChanged += HandleHoverChipChanged;
            HandleHoverChipChanged(cursor.HoveredChip);
        }

        private void OnDisable()
        {
            if (cursor == null) return;
            cursor.OnHoverChipChanged -= HandleHoverChipChanged;
        }

        private void HandleHoverChipChanged(ChipInstance chip)
        {
            if (chip == null)
            {
                if (flatStatText != null) flatStatText.text = "";
                if (percentStatText != null) percentStatText.text = "";
                if (descriptionText != null) descriptionText.text = "";
                return;
            }

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

            if (flatStatText != null) flatStatText.text = string.Join("\n", flatLines);
            if (percentStatText != null) percentStatText.text = string.Join("\n", percentLines);
            if (descriptionText != null) descriptionText.text = chip.Data.Description;
        }

        private static string GetStatDisplayName(ChipStatTarget target)
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
