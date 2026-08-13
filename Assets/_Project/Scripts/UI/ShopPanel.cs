using Manatite.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manatite.UI
{
    /// <summary>
    /// The chip-forging shop panel opened by a <see cref="Manatite.Environment.ShopMarker"/>.
    /// Pauses the game and disables ship input while open, same convention as
    /// <see cref="InventoryMenu"/>/<see cref="PauseMenu"/>. The Merge button is wired but does
    /// nothing yet — this pass only validates the panel's slot/tooltip/picker UI.
    /// </summary>
    public sealed class ShopPanel : MonoBehaviour
    {
        [Tooltip("The ShipControls.inputactions asset containing the ship action map.")]
        [SerializeField] private InputActionAsset controls;
        [SerializeField] private string actionMapName = "Ship";
        [Tooltip("Root of the merge complex — one backpanel behind the 2 merge slots, the Merge " +
            "button, and the tooltip together; hidden until opened.")]
        [SerializeField] private GameObject mergeComplex;
        [Tooltip("Full-screen dim backdrop shown behind the panel while open.")]
        [SerializeField] private GameObject dimBackdrop;
        [Tooltip("Disabled while the panel is open so the reticle/lock indicator don't show over it.")]
        [SerializeField] private Crosshair crosshair;
        [Tooltip("Enabled while the panel is open; drives its own OnEnable/OnDisable reset.")]
        [SerializeField] private ShopCursor cursor;

        private InputActionMap _shipMap;
        private InputAction _cancel;
        private bool _isOpen;

        private void Awake()
        {
            _shipMap = controls.FindActionMap(actionMapName, throwIfNotFound: true);
            _cancel = _shipMap.FindAction("Cancel", throwIfNotFound: true);
        }

        private void OnEnable()
        {
            _cancel.Enable();
            _cancel.performed += OnCancelPerformed;
        }

        private void OnDisable()
        {
            _cancel.performed -= OnCancelPerformed;
            _cancel.Disable();
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
        {
            if (!_isOpen) return;

            if (cursor != null && cursor.IsPickerOpen)
            {
                cursor.ClosePicker();
                return;
            }

            Close();
        }

        public void Open()
        {
            SetOpen(true);
        }

        public void Close()
        {
            SetOpen(false);
        }

        /// <summary>Wired to the Merge button's OnClick. Intentionally empty — merging isn't
        /// implemented yet, this pass only validates the surrounding UI.</summary>
        public void OnMergePressed()
        {
        }

        private void SetOpen(bool open)
        {
            _isOpen = open;
            mergeComplex.SetActive(open);
            if (dimBackdrop != null) dimBackdrop.SetActive(open);
            if (crosshair != null) crosshair.enabled = !open;
            if (cursor != null) cursor.enabled = open;
            Time.timeScale = open ? 0f : 1f;
            AudioListener.pause = open;

            foreach (var action in _shipMap.actions)
            {
                if (action == _cancel) continue;
                if (open) action.Disable();
                else action.Enable();
            }
        }
    }
}
