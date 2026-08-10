using System;
using System.Collections.Generic;
using Manatite.Core;
using Manatite.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Manatite.UI
{
    public sealed class InventoryCursor : MonoBehaviour
    {
        private enum Panel { Equip, Inventory }

        private const int EquipCols = 2;
        private const int EquipRows = 2;
        private const int InventoryCols = 9;
        private const int InventoryRows = 4;

        [Tooltip("The ShipControls.inputactions asset containing the InventoryNav action map.")]
        [SerializeField] private InputActionAsset controls;
        [SerializeField] private string actionMapName = "InventoryNav";
        [Tooltip("Backing data for what's in each slot. Select picks up/places chips here.")]
        [SerializeField] private PlayerInventory inventory;
        [Tooltip("Parent of the 4 equip slots (2 cols x 2 rows).")]
        [SerializeField] private RectTransform equipPanel;
        [Tooltip("Parent of the 36 inventory slots (9 cols x 4 rows).")]
        [SerializeField] private RectTransform inventoryPanel;
        [Tooltip("Steady tint applied to whichever slot the cursor is currently on.")]
        [SerializeField] private Color cursorColor = Color.white;
        [Tooltip("Tint a picked-up slot flashes between while held.")]
        [SerializeField] private Color blinkColor = Color.white;
        [Tooltip("Full on/off cycle length in seconds for the selected-slot blink.")]
        [SerializeField] private float blinkPeriod = 1f;
        [SerializeField] private float moveInitialDelay = 0.35f;
        [SerializeField] private float moveRepeatDelay = 0.15f;
        [SerializeField] private float stickDeadzone = 0.5f;

        private InputActionMap _map;
        private InputAction _navigate;
        private InputAction _select;

        // Cached at Awake since the hover tint and the pick-up blink both overwrite a slot's
        // Image.color at runtime; without this, BeginBlink could capture an already-tinted
        // color as if it were the slot's resting color.
        private readonly Dictionary<Image, Color> _defaultColors = new Dictionary<Image, Color>();

        private Panel _panel;
        private int _row;
        private int _col;
        private float _moveTimer;
        private bool _isRepeating;

        private Image _hoverImage;

        /// <summary>Fired whenever the hovered slot's chip changes (including to/from null), for the tooltip panel.</summary>
        public event Action<ChipInstance> OnHoverChipChanged;
        public ChipInstance HoveredChip { get; private set; }

        private Image _selectedImage;
        private Color _selectedOriginalColor;
        private float _blinkTimer;
        private (bool isEquip, int index)? _heldFrom;

        private void Awake()
        {
            _map = controls.FindActionMap(actionMapName, throwIfNotFound: true);
            _navigate = _map.FindAction("Navigate", throwIfNotFound: true);
            _select = _map.FindAction("Select", throwIfNotFound: true);

            CacheDefaultColors(equipPanel);
            CacheDefaultColors(inventoryPanel);
        }

        private void CacheDefaultColors(RectTransform panel)
        {
            if (panel == null) return;
            for (int i = 0; i < panel.childCount; i++)
            {
                Image img = panel.GetChild(i).GetComponent<Image>();
                if (img != null) _defaultColors[img] = img.color;
            }
        }

        private void OnEnable()
        {
            _map.Enable();
            _select.performed += OnSelectPerformed;

            _panel = Panel.Equip;
            _row = 0;
            _col = 0;
            _moveTimer = 0f;
            _isRepeating = false;

            _hoverImage = null;
            UpdateHoverHighlight();
        }

        private void OnDisable()
        {
            _select.performed -= OnSelectPerformed;
            _map.Disable();
            ClearSelection();
            _heldFrom = null;
            if (_hoverImage != null) _hoverImage.color = _defaultColors[_hoverImage];
            _hoverImage = null;
        }

        private void Update()
        {
            Vector2 dir = _navigate.ReadValue<Vector2>();
            bool hasInput = dir.sqrMagnitude > stickDeadzone * stickDeadzone;

            if (!hasInput)
            {
                _moveTimer = 0f;
                _isRepeating = false;
            }
            else
            {
                if (_moveTimer <= 0f)
                {
                    // Row index increases downward (GridLayoutGroup's UpperLeft start corner) but
                    // stick/dpad +y means up, so the row step is inverted relative to raw input.
                    Vector2Int step = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)
                        ? new Vector2Int((int)Mathf.Sign(dir.x), 0)
                        : new Vector2Int(0, (int)-Mathf.Sign(dir.y));
                    Move(step);
                    _moveTimer = _isRepeating ? moveRepeatDelay : moveInitialDelay;
                    _isRepeating = true;
                }
                _moveTimer -= Time.unscaledDeltaTime;
            }

            UpdateBlink();
        }

        private void Move(Vector2Int step)
        {
            if (step.x != 0) MoveHorizontal(step.x);
            else if (step.y != 0) MoveVertical(step.y);
            UpdateHoverHighlight();
        }

        private void MoveHorizontal(int delta)
        {
            if (_panel == Panel.Equip)
            {
                int newCol = _col + delta;
                if (newCol >= EquipCols)
                {
                    _panel = Panel.Inventory;
                    _col = 0;
                    _row = Mathf.Min(_row, InventoryRows - 1);
                    return;
                }
                _col = Mathf.Max(0, newCol);
            }
            else
            {
                int newCol = _col + delta;
                if (newCol < 0)
                {
                    _panel = Panel.Equip;
                    _col = EquipCols - 1;
                    _row = Mathf.Min(_row, EquipRows - 1);
                    return;
                }
                _col = Mathf.Min(InventoryCols - 1, newCol);
            }
        }

        private void MoveVertical(int delta)
        {
            int rows = _panel == Panel.Equip ? EquipRows : InventoryRows;
            _row = Mathf.Clamp(_row + delta, 0, rows - 1);
        }

        private (bool isEquip, int index) GetCurrentSlotIndex()
        {
            bool isEquip = _panel == Panel.Equip;
            int cols = isEquip ? EquipCols : InventoryCols;
            return (isEquip, _row * cols + _col);
        }

        private RectTransform GetCurrentSlot()
        {
            var (isEquip, index) = GetCurrentSlotIndex();
            RectTransform parent = isEquip ? equipPanel : inventoryPanel;
            return (RectTransform)parent.GetChild(index);
        }

        /// <summary>Tints whichever slot the cursor is currently over; leaves a slot alone
        /// while it's the one actively blinking from a pick-up (that owns its own color).</summary>
        private void UpdateHoverHighlight()
        {
            Image img = GetCurrentSlot().GetComponent<Image>();
            if (img == _hoverImage) return;

            if (_hoverImage != null && _hoverImage != _selectedImage)
                _hoverImage.color = _defaultColors[_hoverImage];

            if (img != _selectedImage)
                img.color = cursorColor;

            _hoverImage = img;

            if (inventory != null)
            {
                var (isEquip, index) = GetCurrentSlotIndex();
                HoveredChip = inventory.GetSlot(isEquip, index);
                OnHoverChipChanged?.Invoke(HoveredChip);
            }
        }

        private void OnSelectPerformed(InputAction.CallbackContext ctx)
        {
            if (inventory == null) return;

            var current = GetCurrentSlotIndex();

            if (_heldFrom == null)
            {
                if (inventory.IsSlotEmpty(current.isEquip, current.index)) return;
                _heldFrom = current;
                BeginBlink(GetCurrentSlot());
                return;
            }

            if (_heldFrom.Value == current)
            {
                ClearSelection();
                _heldFrom = null;
                RefreshHover();
                return;
            }

            inventory.TrySwap(_heldFrom.Value.isEquip, _heldFrom.Value.index, current.isEquip, current.index);
            ClearSelection();
            _heldFrom = null;
            RefreshHover();
        }

        /// <summary>Forces the hover tint to re-apply even though the cursor hasn't moved,
        /// needed after ClearSelection just reverted this same slot's color to default.</summary>
        private void RefreshHover()
        {
            _hoverImage = null;
            UpdateHoverHighlight();
        }

        private void BeginBlink(RectTransform slot)
        {
            Image img = slot.GetComponent<Image>();
            ClearSelection();
            _selectedImage = img;
            _selectedOriginalColor = _defaultColors[img];
            _blinkTimer = 0f;
        }

        private void UpdateBlink()
        {
            if (_selectedImage == null) return;
            _blinkTimer += Time.unscaledDeltaTime;
            float t = _blinkTimer % blinkPeriod;
            _selectedImage.color = t < blinkPeriod * 0.5f ? blinkColor : _selectedOriginalColor;
        }

        private void ClearSelection()
        {
            if (_selectedImage == null) return;
            _selectedImage.color = _selectedOriginalColor;
            _selectedImage = null;
        }
    }
}
