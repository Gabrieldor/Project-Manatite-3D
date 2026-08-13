using Manatite.Core;
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

            ChipTooltipFormatter.BuildStatLines(chip, positiveColor, negativeColor, neutralColor, out string flatText, out string percentText);
            if (flatStatText != null) flatStatText.text = flatText;
            if (percentStatText != null) percentStatText.text = percentText;
            if (descriptionText != null) descriptionText.text = chip.Data.Description;
        }
    }
}
