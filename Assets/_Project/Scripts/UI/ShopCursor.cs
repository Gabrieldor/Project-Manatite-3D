using System;
using System.Collections.Generic;
using Manatite.Core;
using Manatite.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Manatite.UI
{
    /// <summary>
    /// Gamepad-navigable cursor for the shop's Merge row: 2 hoverable chip slots plus the Merge
    /// button, navigated as one horizontal row. Selecting a chip slot reveals the 36-slot
    /// inventory grid (hidden the rest of the time) so the player can pick a chip to place there;
    /// selecting a chip there swaps it into the pending merge slot via
    /// <see cref="PlayerInventory.SwapMergeWithInventory"/> and hides the grid again. Selecting
    /// the Merge button instead invokes its own <c>onClick</c>. Structurally similar to
    /// <see cref="InventoryCursor"/> but a different flow (reveal-then-pick rather than
    /// pick-up-then-place-anywhere), so kept as its own script rather than a retrofit of that
    /// already-shipped one.
    /// </summary>
    public sealed class ShopCursor : MonoBehaviour
    {
        private const int MergeSlotCount = 2;
        // The Merge row is 2 chip slots plus the Merge button, navigable as one horizontal row.
        private const int MergeStopCount = MergeSlotCount + 1;
        private const int InventoryCols = 9;
        private const int InventoryRows = 4;

        [Tooltip("The ShipControls.inputactions asset containing the InventoryNav action map.")]
        [SerializeField] private InputActionAsset controls;
        [SerializeField] private string actionMapName = "InventoryNav";
        [Tooltip("Backing data: merge slots and the player's inventory grid.")]
        [SerializeField] private PlayerInventory inventory;
        [Tooltip("Parent of the 2 merge slots.")]
        [SerializeField] private RectTransform mergePanel;
        [Tooltip("Merge button — the 3rd gamepad-navigable stop in the merge row. Select invokes " +
            "its onClick rather than opening the inventory picker.")]
        [SerializeField] private Button mergeButton;
        [Tooltip("The Merge button's icon Image — its sprite swaps between mergeButtonNormalSprite " +
            "and mergeButtonSelectedSprite on hover instead of being tinted like a slot.")]
        [SerializeField] private Image mergeButtonIcon;
        [SerializeField] private Sprite mergeButtonNormalSprite;
        [SerializeField] private Sprite mergeButtonSelectedSprite;
        [Tooltip("Root shown/hidden as the inventory picker grid is opened/closed.")]
        [SerializeField] private GameObject inventoryPickerRoot;
        [Tooltip("Parent of the 36 inventory picker slots (9 cols x 4 rows).")]
        [SerializeField] private RectTransform inventoryPickerPanel;
        [Tooltip("Steady tint applied to whichever slot the cursor is currently on.")]
        [SerializeField] private Color cursorColor = Color.white;
        [SerializeField] private float moveInitialDelay = 0.35f;
        [SerializeField] private float moveRepeatDelay = 0.15f;
        [SerializeField] private float stickDeadzone = 0.5f;

        private InputActionMap _map;
        private InputAction _navigate;
        private InputAction _select;

        // Cached at Awake since the hover tint overwrites a slot's Image.color at runtime;
        // without this, restoring a slot's resting color after hover would use the wrong value.
        private readonly Dictionary<Image, Color> _defaultColors = new Dictionary<Image, Color>();

        private int _mergeIndex;
        private int _pendingMergeIndex;
        private int _pickerRow;
        private int _pickerCol;
        private float _moveTimer;
        private bool _isRepeating;

        private Image _hoverImage;

        /// <summary>Fired whenever the hovered slot's chip changes (including to/from null), for the tooltip panel.</summary>
        public event Action<ChipInstance> OnHoverChipChanged;
        public ChipInstance HoveredChip { get; private set; }

        /// <summary>Lets the host panel (<see cref="ShopPanel"/>) route Cancel to close just
        /// the picker instead of the whole shop, mirroring PauseMenu/SettingsMenu nesting.</summary>
        public bool IsPickerOpen { get; private set; }

        private void Awake()
        {
            _map = controls.FindActionMap(actionMapName, throwIfNotFound: true);
            _navigate = _map.FindAction("Navigate", throwIfNotFound: true);
            _select = _map.FindAction("Select", throwIfNotFound: true);

            CacheDefaultColors(mergePanel);
            CacheDefaultColors(inventoryPickerPanel);
        }

        private void CacheDefaultColors(RectTransform panel)
        {
            if (panel == null) return;
            for (int i = 0; i < panel.childCount; i++)
                CacheDefaultColor(panel.GetChild(i).GetComponent<Image>());
        }

        private void CacheDefaultColor(Image img)
        {
            if (img != null) _defaultColors[img] = img.color;
        }

        private void OnEnable()
        {
            _map.Enable();
            _select.performed += OnSelectPerformed;

            IsPickerOpen = false;
            if (inventoryPickerRoot != null) inventoryPickerRoot.SetActive(false);
            _mergeIndex = 0;
            _moveTimer = 0f;
            _isRepeating = false;

            _hoverImage = null;
            UpdateHoverHighlight();
        }

        private void OnDisable()
        {
            _select.performed -= OnSelectPerformed;
            _map.Disable();
            if (_hoverImage != null) _hoverImage.color = _defaultColors[_hoverImage];
            _hoverImage = null;
            if (mergeButtonIcon != null) mergeButtonIcon.sprite = mergeButtonNormalSprite;
        }

        private void Update()
        {
            Vector2 dir = _navigate.ReadValue<Vector2>();
            bool hasInput = dir.sqrMagnitude > stickDeadzone * stickDeadzone;

            if (!hasInput)
            {
                _moveTimer = 0f;
                _isRepeating = false;
                return;
            }

            if (_moveTimer <= 0f)
            {
                Vector2Int step = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)
                    ? new Vector2Int((int)Mathf.Sign(dir.x), 0)
                    : new Vector2Int(0, (int)-Mathf.Sign(dir.y));
                Move(step);
                _moveTimer = _isRepeating ? moveRepeatDelay : moveInitialDelay;
                _isRepeating = true;
            }
            _moveTimer -= Time.unscaledDeltaTime;
        }

        private void Move(Vector2Int step)
        {
            if (!IsPickerOpen)
            {
                if (step.x != 0) _mergeIndex = Mathf.Clamp(_mergeIndex + step.x, 0, MergeStopCount - 1);
            }
            else
            {
                if (step.x != 0) _pickerCol = Mathf.Clamp(_pickerCol + step.x, 0, InventoryCols - 1);
                else if (step.y != 0) _pickerRow = Mathf.Clamp(_pickerRow + step.y, 0, InventoryRows - 1);
            }
            UpdateHoverHighlight();
        }

        private RectTransform GetCurrentSlot()
        {
            if (IsPickerOpen) return (RectTransform)inventoryPickerPanel.GetChild(_pickerRow * InventoryCols + _pickerCol);
            if (_mergeIndex < MergeSlotCount) return (RectTransform)mergePanel.GetChild(_mergeIndex);
            return mergeButton.GetComponent<RectTransform>();
        }

        private ChipInstance GetCurrentChip()
        {
            if (IsPickerOpen) return inventory.GetSlot(false, _pickerRow * InventoryCols + _pickerCol);
            if (_mergeIndex < MergeSlotCount) return inventory.GetMergeSlot(_mergeIndex);
            return null;
        }

        private void UpdateHoverHighlight()
        {
            if (inventory == null) return;

            bool hoveringButton = !IsPickerOpen && _mergeIndex >= MergeSlotCount;

            if (hoveringButton)
            {
                // The button swaps to a dedicated "selected" sprite instead of being tinted like
                // a slot — flatly recoloring the icon would wash out its own art.
                if (_hoverImage != null)
                {
                    _hoverImage.color = _defaultColors[_hoverImage];
                    _hoverImage = null;
                }
                if (mergeButtonIcon != null) mergeButtonIcon.sprite = mergeButtonSelectedSprite;
            }
            else
            {
                if (mergeButtonIcon != null) mergeButtonIcon.sprite = mergeButtonNormalSprite;

                Image img = GetCurrentSlot().GetComponent<Image>();
                if (img != _hoverImage)
                {
                    if (_hoverImage != null) _hoverImage.color = _defaultColors[_hoverImage];
                    img.color = cursorColor;
                    _hoverImage = img;
                }
            }

            HoveredChip = GetCurrentChip();
            OnHoverChipChanged?.Invoke(HoveredChip);
        }

        private void OnSelectPerformed(InputAction.CallbackContext ctx)
        {
            if (inventory == null) return;

            if (!IsPickerOpen)
            {
                if (_mergeIndex < MergeSlotCount) OpenPicker(_mergeIndex);
                else if (mergeButton != null) mergeButton.onClick.Invoke();
                return;
            }

            int inventoryIndex = _pickerRow * InventoryCols + _pickerCol;
            inventory.SwapMergeWithInventory(_pendingMergeIndex, inventoryIndex);
            ClosePicker();
        }

        private void OpenPicker(int mergeIndex)
        {
            _pendingMergeIndex = mergeIndex;
            IsPickerOpen = true;
            if (inventoryPickerRoot != null) inventoryPickerRoot.SetActive(true);
            _pickerRow = 0;
            _pickerCol = 0;
            RefreshHover();
        }

        /// <summary>Closes the picker without swapping anything, returning focus to the merge
        /// slot it was opened from. Called both after a successful pick and by the host panel
        /// when Cancel is pressed while the picker is open.</summary>
        public void ClosePicker()
        {
            if (!IsPickerOpen) return;
            IsPickerOpen = false;
            if (inventoryPickerRoot != null) inventoryPickerRoot.SetActive(false);
            _mergeIndex = _pendingMergeIndex;
            RefreshHover();
        }

        /// <summary>Wired to the Merge button's OnClick via <see cref="ShopPanel"/>. Refreshes
        /// the hover afterward since slot 0 now holds the merged result and slot 1 is empty.</summary>
        public void TryMerge()
        {
            if (inventory == null || !inventory.TryMerge()) return;
            RefreshHover();
        }

        /// <summary>Forces the hover tint/tooltip to re-apply even though the cursor hasn't
        /// moved, needed whenever the underlying panel (merge row vs. picker grid) changes.</summary>
        private void RefreshHover()
        {
            _hoverImage = null;
            UpdateHoverHighlight();
        }
    }
}
