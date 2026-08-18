using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Manatite.UI
{
    /// <summary>
    /// One-time controls guide shown the first time the player clicks New Game. Gamepad/Keyboard
    /// tabs swap which guide text block is visible; Continue dismisses and marks it seen so it
    /// never shows again on this install.
    /// </summary>
    public sealed class ControlsTooltip : MonoBehaviour
    {
        private const string SeenPrefsKey = "Manatite.ControlsTooltipSeen";

        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject gamepadGuideText;
        [SerializeField] private GameObject keyboardGuideText;
        [Tooltip("Selected by default when the panel opens (Gamepad tab).")]
        [SerializeField] private Button gamepadTabButton;
        [SerializeField] private Button keyboardTabButton;
        [SerializeField] private Button continueButton;
        [Tooltip("This project's buttons don't reliably pick up Selectable's built-in ColorTint " +
            "transition at runtime, so the selected button's highlight is driven directly here " +
            "instead of trusting Button.colors.")]
        [SerializeField] private Color restingColor = new Color(0.2784314f, 0.9058824f, 0.7568628f, 0.88f);
        [SerializeField] private Color selectedColor = new Color(0.098122105f, 0.990566f, 0.78169614f, 1f);

        private Action _onContinue;
        private GameObject _lastSelected;

        public static bool HasBeenSeen => PlayerPrefs.GetInt(SeenPrefsKey, 0) != 0;

        public void Show(Action onContinue)
        {
            _onContinue = onContinue;
            panel.SetActive(true);
            SelectGamepadTab();

            GameObject select = gamepadTabButton != null ? gamepadTabButton.gameObject : null;
            if (EventSystem.current != null && select != null)
                EventSystem.current.SetSelectedGameObject(select);

            _lastSelected = select;
            RefreshHighlights(select);
        }

        // Tabs swap on selection alone (no Submit needed) — moving onto a tab via gamepad/keyboard
        // navigation should preview its guide immediately, matching how a tab strip normally reads.
        // Also drives the selected-button highlight directly (see restingColor/selectedColor above).
        private void LateUpdate()
        {
            if (panel == null || !panel.activeSelf) return;
            if (EventSystem.current == null) return;

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == _lastSelected) return;
            _lastSelected = selected;

            RefreshHighlights(selected);

            if (gamepadTabButton != null && selected == gamepadTabButton.gameObject) SelectGamepadTab();
            else if (keyboardTabButton != null && selected == keyboardTabButton.gameObject) SelectKeyboardTab();
        }

        private void RefreshHighlights(GameObject selected)
        {
            SetHighlight(gamepadTabButton, selected);
            SetHighlight(keyboardTabButton, selected);
            SetHighlight(continueButton, selected);
        }

        private void SetHighlight(Button button, GameObject selected)
        {
            if (button == null || button.targetGraphic == null) return;
            button.targetGraphic.color = button.gameObject == selected ? selectedColor : restingColor;
        }

        public void SelectGamepadTab()
        {
            gamepadGuideText.SetActive(true);
            keyboardGuideText.SetActive(false);
        }

        public void SelectKeyboardTab()
        {
            gamepadGuideText.SetActive(false);
            keyboardGuideText.SetActive(true);
        }

        public void OnContinue()
        {
            PlayerPrefs.SetInt(SeenPrefsKey, 1);
            PlayerPrefs.Save();
            panel.SetActive(false);

            Action callback = _onContinue;
            _onContinue = null;
            callback?.Invoke();
        }
    }
}
