using UnityEngine;
using UnityEngine.InputSystem;

namespace Manatite.UI
{
    public sealed class InventoryMenu : MonoBehaviour
    {
        [Tooltip("The ShipControls.inputactions asset containing the ship action map.")]
        [SerializeField] private InputActionAsset controls;
        [SerializeField] private string actionMapName = "Ship";
        [Tooltip("Root of the 4-slot equip panel (left third of the screen).")]
        [SerializeField] private GameObject equipPanel;
        [Tooltip("Root of the 36-slot inventory panel (right two-thirds of the screen).")]
        [SerializeField] private GameObject inventoryPanel;
        [Tooltip("Disabled while the inventory is open so the reticle/lock indicator don't show over it.")]
        [SerializeField] private Crosshair crosshair;
        [Tooltip("Enabled while the inventory is open; drives its own OnEnable/OnDisable reset.")]
        [SerializeField] private InventoryCursor cursor;

        private InputActionMap _shipMap;
        private InputAction _toggle;
        private InputAction _cancel;
        private bool _isOpen;

        private void Awake()
        {
            _shipMap = controls.FindActionMap(actionMapName, throwIfNotFound: true);
            _toggle = _shipMap.FindAction("Inventory", throwIfNotFound: true);
            _cancel = _shipMap.FindAction("Cancel", throwIfNotFound: true);
        }

        private void OnEnable()
        {
            _toggle.Enable();
            _toggle.performed += OnTogglePerformed;
            _cancel.Enable();
            _cancel.performed += OnCancelPerformed;
        }

        private void OnDisable()
        {
            _toggle.performed -= OnTogglePerformed;
            _toggle.Disable();
            _cancel.performed -= OnCancelPerformed;
            _cancel.Disable();
        }

        private void OnTogglePerformed(InputAction.CallbackContext ctx)
        {
            SetOpen(!_isOpen);
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
        {
            if (_isOpen) SetOpen(false);
        }

        private void SetOpen(bool open)
        {
            _isOpen = open;
            equipPanel.SetActive(open);
            inventoryPanel.SetActive(open);
            if (crosshair != null) crosshair.enabled = !open;
            if (cursor != null) cursor.enabled = open;
            Time.timeScale = open ? 0f : 1f;
            AudioListener.pause = open;

            foreach (var action in _shipMap.actions)
            {
                if (action == _toggle || action == _cancel) continue;
                if (open) action.Disable();
                else action.Enable();
            }
        }
    }
}
