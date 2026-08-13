using Manatite.Core;
using Manatite.UI;
using UnityEngine;

namespace Manatite.Environment
{
    /// <summary>
    /// World-space chip-forging shop marker: billboards to face the camera and opens the
    /// shop panel when the player flies into its trigger. <see cref="Initialize"/> must be
    /// called by whatever spawns it (a cleared planet's loot roll, or a debug spawner) since
    /// the panel it opens is a scene object a prefab can't pre-reference.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ShopMarker : MonoBehaviour
    {
        private ShopPanel _shopPanel;
        private Transform _cameraTransform;

        public void Initialize(ShopPanel shopPanel)
        {
            _shopPanel = shopPanel;
        }

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - _cameraTransform.position, Vector3.up);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_shopPanel == null) return;
            Rigidbody rb = other.attachedRigidbody;
            if (rb == null || !rb.CompareTag(GameLayers.PlayerTag)) return;

            _shopPanel.Open();
        }
    }
}
