using UnityEngine;

namespace Manatite.UI
{
    /// <summary>Decorative idle float for the Main Menu's display ship — bobs it up and
    /// down in local Y via a sine wave, offset from whatever position it starts at.</summary>
    public sealed class MenuShipBob : MonoBehaviour
    {
        [Tooltip("Peak distance (m) above/below the starting position.")]
        [SerializeField] private float amplitude = 1.5f;
        [Tooltip("Full up-down-up cycles per second.")]
        [SerializeField] private float frequency = 0.25f;

        private Vector3 _basePosition;

        private void Start()
        {
            _basePosition = transform.localPosition;
        }

        private void Update()
        {
            float offset = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) * amplitude;
            transform.localPosition = _basePosition + Vector3.up * offset;
        }
    }
}
