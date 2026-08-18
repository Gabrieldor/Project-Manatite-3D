using UnityEngine;
using UnityEngine.InputSystem;

namespace Manatite.UI
{
    /// <summary>
    /// Shows the first-time controls tooltip as soon as gameplay starts, freezing the ship's
    /// input and physics until dismissed — same freeze mechanism <see cref="PauseMenu"/> uses.
    /// No-ops (leaves the game running) once <see cref="ControlsTooltip.HasBeenSeen"/> is true.
    /// </summary>
    public sealed class GameplayControlsIntro : MonoBehaviour
    {
        [SerializeField] private InputActionAsset controls;
        [SerializeField] private string actionMapName = "Ship";
        [SerializeField] private ControlsTooltip controlsTooltip;

        private InputActionMap _shipMap;

        private void Awake()
        {
            _shipMap = controls.FindActionMap(actionMapName, throwIfNotFound: true);
        }

        private void Start()
        {
            if (controlsTooltip == null || ControlsTooltip.HasBeenSeen) return;

            Time.timeScale = 0f;
            AudioListener.pause = true;
            _shipMap.Disable();
            controlsTooltip.Show(OnDismissed);
        }

        private void OnDismissed()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            _shipMap.Enable();
        }
    }
}
