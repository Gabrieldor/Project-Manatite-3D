using Manatite.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Manatite.UI
{
    public sealed class MenuCursor : MonoBehaviour
    {
        private GameObject _lastSelected;

        // Every Button under this menu's root gets a press cue for free, gamepad Submit and
        // mouse click alike (both funnel through Button.onClick).
        private void Awake()
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
                button.onClick.AddListener(PlayConfirm);
        }

        private static void PlayConfirm() => AudioManager.PlaySfx(SfxCue.Confirm);

        private void LateUpdate()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return;

            GameObject selected = eventSystem.currentSelectedGameObject;

            // Clicking empty space (anything without an ISelectHandler) deselects everything,
            // which then strands gamepad/keyboard navigation with nothing to move from. Reselect
            // the last button as long as it's still on-screen (an intentional deselect, e.g. on
            // menu close, always hides that button's panel first, so this won't fight those).
            if (selected == null && _lastSelected != null && _lastSelected.activeInHierarchy)
            {
                eventSystem.SetSelectedGameObject(_lastSelected);
                return;
            }

            if (selected == _lastSelected) return;
            _lastSelected = selected;

            if (selected == null) return;
            AudioManager.PlaySfx(SfxCue.Hover);
        }
    }
}
