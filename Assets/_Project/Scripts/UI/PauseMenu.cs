using Manatite.Core.SaveSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Manatite.UI
{
    public sealed class PauseMenu : MonoBehaviour
    {
        [Tooltip("The ShipControls.inputactions asset containing the ship action map.")]
        [SerializeField] private InputActionAsset controls;
        [SerializeField] private string actionMapName = "Ship";
        [Tooltip("Root of the visible menu panel; hidden until paused.")]
        [SerializeField] private GameObject panel;
        [Tooltip("Full-screen dim backdrop shown behind the panel while paused.")]
        [SerializeField] private GameObject dimBackdrop;
        [Tooltip("Selected by default when the menu opens, for gamepad navigation.")]
        [SerializeField] private Button defaultSelected;
        [SerializeField] private RunSaveController saveController;
        [Tooltip("Disabled unless a save file exists; re-checked every time the menu opens.")]
        [SerializeField] private Button loadGameButton;
        [SerializeField] private string gameplaySceneName = "Outer Space";
        [Tooltip("If open, Cancel closes this instead of unpausing.")]
        [SerializeField] private SettingsMenu settingsMenu;

        /// <summary>Matches the alpha every always-enabled menu button was authored with.
        /// This scene's disabled-looking buttons dim via a hand-painted Image alpha, not
        /// Selectable's color-tint transitions, so flipping interactable alone doesn't
        /// visually re-enable a button — the alpha has to move too.</summary>
        private const float EnabledButtonAlpha = 0.88f;

        private InputActionMap _shipMap;
        private InputAction _pause;
        private InputAction _cancel;
        private bool _isPaused;

        private void Awake()
        {
            _shipMap = controls.FindActionMap(actionMapName, throwIfNotFound: true);
            _pause = _shipMap.FindAction("Pause", throwIfNotFound: true);
            _cancel = _shipMap.FindAction("Cancel", throwIfNotFound: true);
        }

        private void OnEnable()
        {
            _pause.Enable();
            _pause.performed += OnPausePerformed;
            _cancel.Enable();
            _cancel.performed += OnCancelPerformed;
        }

        private void OnDisable()
        {
            _pause.performed -= OnPausePerformed;
            _pause.Disable();
            _cancel.performed -= OnCancelPerformed;
            _cancel.Disable();
        }

        private void OnPausePerformed(InputAction.CallbackContext ctx)
        {
            SetPaused(!_isPaused);
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
        {
            if (settingsMenu != null && settingsMenu.IsOpen)
            {
                settingsMenu.Close();
                return;
            }

            // Escape is bound to both Pause and Cancel, so a keyboard Esc press fires both
            // handlers — Pause's own toggle already closes the panel, so skip here to avoid
            // racing it (Cancel closes, then Pause's toggle immediately reopens). Other Cancel
            // bindings (gamepad East) have no such overlap and still close the panel directly.
            if (ctx.control is UnityEngine.InputSystem.Controls.KeyControl keyControl
                && keyControl.keyCode == Key.Escape)
                return;

            if (_isPaused) SetPaused(false);
        }

        private void SetPaused(bool paused)
        {
            _isPaused = paused;
            panel.SetActive(paused);
            if (dimBackdrop != null) dimBackdrop.SetActive(paused);
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;

            if (paused)
                SetLoadGameInteractable(SaveFile.HasSave());

            foreach (var action in _shipMap.actions)
            {
                if (action == _pause || action == _cancel) continue;
                if (paused) action.Disable();
                else action.Enable();
            }

            if (EventSystem.current != null)
            {
                if (paused && defaultSelected != null)
                    EventSystem.current.SetSelectedGameObject(defaultSelected.gameObject);
                else if (!paused)
                    EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void SetLoadGameInteractable(bool value)
        {
            if (loadGameButton == null) return;

            loadGameButton.interactable = value;
            if (!value) return;

            if (loadGameButton.image != null)
            {
                Color color = loadGameButton.image.color;
                color.a = EnabledButtonAlpha;
                loadGameButton.image.color = color;
            }

            var label = loadGameButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null)
            {
                Color labelColor = label.color;
                labelColor.a = 1f;
                label.color = labelColor;
            }
        }

        public void OnSaveGame()
        {
            if (saveController != null) saveController.Save();
        }

        public void OnLoadGame()
        {
            if (!SaveFile.TryLoad(out SaveData data)) return;

            PendingLoad.Data = data;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void OnQuitToMainMenu()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene("Main Menu");
        }
    }
}
