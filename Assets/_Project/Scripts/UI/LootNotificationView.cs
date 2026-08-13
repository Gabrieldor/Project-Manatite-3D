using System.Collections;
using System.Collections.Generic;
using Manatite.Core;
using Manatite.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Manatite.UI
{
    /// <summary>
    /// Pops up a brief toast (icon + name + full stat breakdown + description) whenever a
    /// chip drops into the player's inventory, e.g. from a cleared planet's loot table.
    /// Simultaneous drops queue and show one after another rather than overlapping.
    /// </summary>
    public sealed class LootNotificationView : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI flatStatText;
        [SerializeField] private TextMeshProUGUI percentStatText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private float displayDuration = 2.5f;
        [SerializeField] private Color positiveColor = Color.green;
        [SerializeField] private Color negativeColor = Color.red;
        [SerializeField] private Color neutralColor = Color.white;

        private readonly Queue<ChipInstance> _queue = new();
        private Coroutine _routine;

        private void OnEnable()
        {
            if (inventory != null) inventory.OnItemAdded += HandleItemAdded;
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.OnItemAdded -= HandleItemAdded;
        }

        private void HandleItemAdded(ChipInstance chip)
        {
            _queue.Enqueue(chip);
            if (_routine == null) _routine = StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            while (_queue.Count > 0)
            {
                ChipInstance chip = _queue.Dequeue();

                if (iconImage != null)
                {
                    iconImage.sprite = chip.Data.Icon;
                    iconImage.enabled = chip.Data.Icon != null;
                }
                if (nameText != null) nameText.text = chip.Data.DisplayName;

                ChipTooltipFormatter.BuildStatLines(chip, positiveColor, negativeColor, neutralColor, out string flatText, out string percentText);
                if (flatStatText != null) flatStatText.text = flatText;
                if (percentStatText != null) percentStatText.text = percentText;
                if (descriptionText != null) descriptionText.text = chip.Data.Description;

                if (panelRoot != null) panelRoot.SetActive(true);
                yield return new WaitForSeconds(displayDuration);
                if (panelRoot != null) panelRoot.SetActive(false);
            }
            _routine = null;
        }
    }
}
