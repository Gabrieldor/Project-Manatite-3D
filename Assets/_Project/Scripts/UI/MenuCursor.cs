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

        private void LateUpdate()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == _lastSelected) return;
            _lastSelected = selected;

            if (_tintedGraphic != null)
            {
                _tintedGraphic.color = _tintedOriginalColor;
                _tintedGraphic = null;
            }

            if (selected == null) return;

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
