using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Manatite.UI
{
    public sealed class InventoryCursor : MonoBehaviour
    {
        private enum Panel { Equip, Inventory }

        private const int EquipCols = 2;
        private const int EquipRows = 3;
        private const int InventoryCols = 9;
        private const int InventoryRows = 4;

        [Tooltip("The ShipControls.inputactions asset containing the InventoryNav action map.")]
        [SerializeField] private InputActionAsset controls;
        [SerializeField] private string actionMapName = "InventoryNav";
        [Tooltip("The slotcursor Image that moves between slots.")]
        [SerializeField] private RectTransform cursorImage;
        [Tooltip("Parent of the 6 equip slots (2 cols x 3 rows).")]
        [SerializeField] private RectTransform equipPanel;
        [Tooltip("Parent of the 36 inventory slots (9 cols x 4 rows).")]
        [SerializeField] private RectTransform inventoryPanel;
        [SerializeField] private Color blinkColor = Color.white;
        [Tooltip("Full on/off cycle length in seconds for the selected-slot blink.")]
        [SerializeField] private float blinkPeriod = 1f;
        [SerializeField] private float moveInitialDelay = 0.35f;
        [SerializeField] private float moveRepeatDelay = 0.15f;
        [SerializeField] private float stickDeadzone = 0.5f;

        private InputActionMap _map;
        private InputAction _navigate;
        private InputAction _select;

        private Panel _panel;
        private int _row;
        private int _col;
        private float _moveTimer;
        private bool _isRepeating;

        private Image _selectedImage;
        private Color _selectedOriginalColor;
        private float _blinkTimer;

        private void Awake()
        {
            _map = controls.FindActionMap(actionMapName, throwIfNotFound: true);
            _navigate = _map.FindAction("Navigate", throwIfNotFound: true);
            _select = _map.FindAction("Select", throwIfNotFound: true);
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

            if (cursorImage != null)
            {
                // Panels were just activated this same frame by InventoryMenu; their GridLayoutGroups
                // haven't rebuilt yet, so slot rects are still stale until forced here.
                if (equipPanel != null) LayoutRebuilder.ForceRebuildLayoutImmediate(equipPanel);
                if (inventoryPanel != null) LayoutRebuilder.ForceRebuildLayoutImmediate(inventoryPanel);
                cursorImage.gameObject.SetActive(true);
                UpdateCursorVisual();
            }
        }

        private void OnDisable()
        {
            _select.performed -= OnSelectPerformed;
            _map.Disable();
            ClearSelection();
            if (cursorImage != null) cursorImage.gameObject.SetActive(false);
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
            UpdateCursorVisual();
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

        private RectTransform GetCurrentSlot()
        {
            RectTransform parent = _panel == Panel.Equip ? equipPanel : inventoryPanel;
            int cols = _panel == Panel.Equip ? EquipCols : InventoryCols;
            int index = _row * cols + _col;
            return (RectTransform)parent.GetChild(index);
        }

        private void UpdateCursorVisual()
        {
            if (cursorImage == null) return;
            RectTransform slot = GetCurrentSlot();
            cursorImage.position = slot.position;
            cursorImage.sizeDelta = slot.sizeDelta;
        }

        private void OnSelectPerformed(InputAction.CallbackContext ctx)
        {
            RectTransform slot = GetCurrentSlot();
            Image img = slot.GetComponent<Image>();

            if (_selectedImage == img)
            {
                ClearSelection();
                return;
            }

            ClearSelection();
            _selectedImage = img;
            _selectedOriginalColor = img.color;
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
