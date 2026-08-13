using Manatite.Core;
using TMPro;
using UnityEngine;

namespace Manatite.UI
{
    /// <summary>
    /// Same layout/behavior as <see cref="TooltipView"/> but bound to <see cref="ShopCursor"/>
    /// instead of <see cref="InventoryCursor"/> — the shop panel is a separate screen with its
    /// own tooltip UI, not a shared instance with the main Inventory Menu.
    /// </summary>
    public sealed class ShopTooltipView : MonoBehaviour
    {
        [SerializeField] private ShopCursor cursor;
        [Tooltip("Left half: one line per Flat modifier on the hovered chip.")]
        [SerializeField] private TextMeshProUGUI flatStatText;
        [Tooltip("Right half: one line per Multiplier modifier on the hovered chip.")]
        [SerializeField] private TextMeshProUGUI percentStatText;
        [Tooltip("ChipData.Description verbatim.")]
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

            ChipTooltipFormatter.BuildStatLines(chip, positiveColor, negativeColor, neutralColor, out string flatText, out string percentText);
            if (flatStatText != null) flatStatText.text = flatText;
            if (percentStatText != null) percentStatText.text = percentText;
            if (descriptionText != null) descriptionText.text = chip.Data.Description;
        }
    }
}
