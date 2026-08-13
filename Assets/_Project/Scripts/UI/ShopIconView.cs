using Manatite.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Manatite.UI
{
    /// <summary>
    /// Same icon-rendering approach as <see cref="InventoryIconView"/> but for the shop's
    /// Merge slots and inventory picker grid, which read from
    /// <see cref="PlayerInventory.GetMergeSlot"/> instead of the equip/inventory arrays.
    /// </summary>
    public sealed class ShopIconView : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [Tooltip("Parent of the 2 merge slots.")]
        [SerializeField] private RectTransform mergePanel;
        [Tooltip("Parent of the 36 inventory picker slots.")]
        [SerializeField] private RectTransform inventoryPickerPanel;
        [Tooltip("Icon size as a fraction of its own slot's size.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float iconFillFraction = 0.64f;
        [Tooltip("Vertical nudge (px) applied to inventory picker slot icons only.")]
        [SerializeField] private float inventoryIconYOffset = 8f;

        private Image[] _mergeIcons;
        private Image[] _inventoryIcons;

        private void Awake()
        {
            _mergeIcons = BuildIcons(mergePanel, iconFillFraction, 0f);
            _inventoryIcons = BuildIcons(inventoryPickerPanel, iconFillFraction, inventoryIconYOffset);
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.OnInventoryChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.OnInventoryChanged -= Refresh;
        }

        private static Image[] BuildIcons(RectTransform panel, float fillFraction, float yOffset)
        {
            if (panel == null) return System.Array.Empty<Image>();

            float inset = (1f - fillFraction) * 0.5f;
            var icons = new Image[panel.childCount];
            for (int i = 0; i < panel.childCount; i++)
            {
                var slot = (RectTransform)panel.GetChild(i);
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                var iconRect = (RectTransform)iconGo.transform;
                iconRect.SetParent(slot, false);
                iconRect.anchorMin = new Vector2(inset, inset);
                iconRect.anchorMax = new Vector2(1f - inset, 1f - inset);
                iconRect.offsetMin = new Vector2(0f, yOffset);
                iconRect.offsetMax = new Vector2(0f, yOffset);

                var image = iconGo.GetComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
                image.enabled = false;
                icons[i] = image;
            }
            return icons;
        }

        private void Refresh()
        {
            if (inventory == null) return;

            for (int i = 0; i < _mergeIcons.Length; i++)
            {
                var chip = inventory.GetMergeSlot(i);
                _mergeIcons[i].sprite = chip?.Data.Icon;
                _mergeIcons[i].enabled = chip != null;
            }

            for (int i = 0; i < _inventoryIcons.Length; i++)
            {
                var chip = inventory.GetSlot(false, i);
                _inventoryIcons[i].sprite = chip?.Data.Icon;
                _inventoryIcons[i].enabled = chip != null;
            }
        }
    }
}
