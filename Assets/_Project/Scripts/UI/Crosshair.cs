using UnityEngine;
using UnityEngine.UI;
using Manatite.Player;

namespace Manatite.UI
{
    public class Crosshair : MonoBehaviour
    {
        [SerializeField] private TargetLock targetLock;
        [SerializeField] private Camera hudCamera;
        [SerializeField] private Image centerReticle;
        [SerializeField] private RectTransform lockIndicator;
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color lockedColor = Color.red;

        private void Update()
        {
            bool locked = targetLock.IsLocked;

            // Update center reticle color based on lock state
            if (centerReticle != null)
            {
                centerReticle.color = locked ? lockedColor : unlockedColor;
            }

            // Show/hide lock indicator
            if (lockIndicator != null)
            {
                if (locked)
                {
                    // Target is locked: compute screen position and track it
                    Transform currentTarget = targetLock.CurrentTarget;
                    if (currentTarget != null)
                    {
                        Vector3 worldPos = currentTarget.position;
                        Vector3 viewportPoint = hudCamera.WorldToViewportPoint(worldPos);

                        // Only position if target is in front of camera (z > 0)
                        if (viewportPoint.z > 0)
                        {
                            // Convert viewport point to screen coordinates
                            Vector3 screenPoint = new Vector3(
                                viewportPoint.x * Screen.width,
                                viewportPoint.y * Screen.height,
                                0
                            );
                            lockIndicator.position = screenPoint;
                            lockIndicator.gameObject.SetActive(true);
                        }
                        else
                        {
                            // Target is behind camera, hide the indicator
                            lockIndicator.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        lockIndicator.gameObject.SetActive(false);
                    }
                }
                else
                {
                    // Not locked: hide the indicator
                    lockIndicator.gameObject.SetActive(false);
                }
            }
        }
    }
}
