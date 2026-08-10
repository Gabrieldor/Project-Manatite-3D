using Manatite.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Manatite.UI
{
    /// <summary>
    /// Shows each equip/inventory slot's chip icon, if any. Icon Images are instantiated at
    /// runtime as a child of every existing slot rather than hand-placed in the scene.
    /// </summary>
    public sealed class InventoryIconView : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [Tooltip("Parent of the 4 equip slots.")]
        [SerializeField] private RectTransform equipPanel;
        [Tooltip("Parent of the 36 inventory slots.")]
        [SerializeField] private RectTransform inventoryPanel;
        [Tooltip("Icon size as a fraction of its own slot's size (not a fixed pixel size, since equip and inventory slots are different sizes).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float iconFillFraction = 0.64f;
        [Tooltip("Vertical nudge (px) applied to inventory slot icons only.")]
        [SerializeField] private float inventoryIconYOffset = 8f;

        private Image[] _equipIcons;
        private Image[] _inventoryIcons;

        private void Awake()
        {
            _equipIcons = BuildIcons(equipPanel, iconFillFraction, 0f);
            _inventoryIcons = BuildIcons(inventoryPanel, iconFillFraction, inventoryIconYOffset);
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
            RefreshPanel(_equipIcons, true);
            RefreshPanel(_inventoryIcons, false);
        }

        private void RefreshPanel(Image[] icons, bool isEquip)
        {
            for (int i = 0; i < icons.Length; i++)
            {
                var chip = inventory.GetSlot(isEquip, i);
                icons[i].sprite = chip?.Data.Icon;
                icons[i].enabled = chip != null;
            }
        }
    }
}
