using Manatite.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Manatite.UI
{
    public sealed class MenuCursor : MonoBehaviour
    {
        [Tooltip("Steady tint applied to whichever button is currently selected.")]
        [SerializeField] private Color selectedColor = Color.white;

        private GameObject _lastSelected;
        private Graphic _tintedGraphic;
        private Color _tintedOriginalColor;

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

            if (_tintedGraphic != null)
            {
                _tintedGraphic.color = _tintedOriginalColor;
                _tintedGraphic = null;
            }

            if (selected == null) return;
            AudioManager.PlaySfx(SfxCue.Hover);

            // A SelectionGlowTarget lets a selected object (e.g. a Slider) redirect the glow
            // onto a different Graphic (e.g. its row's label) instead of tinting itself.
            Graphic graphic = selected.GetComponent<SelectionGlowTarget>()?.Target;
            if (graphic == null) graphic = selected.GetComponent<Image>();
            if (graphic == null) return;

            _tintedGraphic = graphic;
            _tintedOriginalColor = graphic.color;
            graphic.color = selectedColor;
        }

        private void OnDisable()
        {
            if (_tintedGraphic == null) return;
            _tintedGraphic.color = _tintedOriginalColor;
            _tintedGraphic = null;
        }
    }
}
