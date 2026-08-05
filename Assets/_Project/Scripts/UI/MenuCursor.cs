using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Manatite.UI
{
    public sealed class MenuCursor : MonoBehaviour
    {
        [Tooltip("The arrow Image that points at the currently selected button.")]
        [SerializeField] private RectTransform arrow;
        [Tooltip("Offset from the left edge of the selected button's label text, in canvas pixels.")]
        [SerializeField] private Vector2 offset = new Vector2(-16f, 0f);

        private GameObject _lastSelected;

        private void LateUpdate()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == _lastSelected) return;
            _lastSelected = selected;

            if (arrow == null) return;

            if (selected == null)
            {
                arrow.gameObject.SetActive(false);
                return;
            }

            RectTransform selectedRect = selected.GetComponent<RectTransform>();
            if (selectedRect == null)
            {
                arrow.gameObject.SetActive(false);
                return;
            }

            // The layout group driving this button may not have rebuilt yet this frame
            // (e.g. right after the menu loads), which would leave its rect stale.
            Canvas.ForceUpdateCanvases();

            arrow.gameObject.SetActive(true);

            // Labels are center-aligned within the button, so the text's actual left edge
            // depends on its own width, not the (fixed-width) button rect.
            RectTransform anchorRect = selectedRect;
            float leftEdgeInset = 0f;
            TextMeshProUGUI label = selected.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                anchorRect = label.rectTransform;
                leftEdgeInset = -label.GetPreferredValues().x * 0.5f;
            }

            Vector3 worldOffset = arrow.parent != null
                ? arrow.parent.TransformVector(new Vector3(leftEdgeInset + offset.x, offset.y, 0f))
                : new Vector3(leftEdgeInset + offset.x, offset.y, 0f);
            arrow.position = anchorRect.position + worldOffset;
        }
    }
}
