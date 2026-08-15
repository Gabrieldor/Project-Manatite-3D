using UnityEngine;

namespace Manatite.UI
{
    /// <summary>Slides the Main Menu blackout panel clear of the camera in discrete jumps
    /// (no interpolation between jumps) so the reveal reads as stepped, not smooth.</summary>
    public sealed class MenuBlackoutReveal : MonoBehaviour
    {
        [Tooltip("Local-space direction the blackout slides to clear the camera view.")]
        [SerializeField] private Vector3 moveDirection = Vector3.right;

        [Tooltip("Total distance (m) travelled once every step has fired.")]
        [SerializeField] private float travelDistance = 30f;

        [Tooltip("Number of discrete jumps to cover the full travel distance.")]
        [SerializeField] private int stepCount = 8;

        [Tooltip("Frames per jump, timed against referenceFrameRate below.")]
        [SerializeField] private int framesPerStep = 8;

        [Tooltip("Frame rate the framesPerStep count is timed against.")]
        [SerializeField] private float referenceFrameRate = 24f;

        [Tooltip("Ship exhaust particle — hidden at the start of the reveal, restored once the blackout clears the threshold below.")]
        [SerializeField] private GameObject shipParticle;

        [Tooltip("Main menu UI — activated once the blackout clears the threshold below.")]
        [SerializeField] private GameObject mainMenuCanvas;

        [Tooltip("Post-processing volume — activated once the blackout clears the threshold below.")]
        [SerializeField] private GameObject globalVolume;

        [Tooltip("Ship idle bob — held still during the reveal, resumed once the blackout clears the threshold below.")]
        [SerializeField] private MenuShipBob shipBob;

        [Tooltip("Local X position the blackout must pass (moving negative) before the menu/volume/particle are revealed.")]
        [SerializeField] private float revealXThreshold = -18f;

        public bool IsRevealed => _revealed;

        private Vector3 _startLocalPosition;
        private Vector3 _endLocalPosition;
        private float _secondsPerStep;
        private float _timer;
        private int _stepsTaken;
        private bool _revealed;

        private void Start()
        {
            _startLocalPosition = transform.localPosition;
            _endLocalPosition = _startLocalPosition + moveDirection.normalized * travelDistance;
            _secondsPerStep = framesPerStep / referenceFrameRate;

            if (shipParticle != null)
            {
                shipParticle.SetActive(false);
            }

            if (mainMenuCanvas != null)
            {
                mainMenuCanvas.SetActive(false);
            }

            if (globalVolume != null)
            {
                globalVolume.SetActive(false);
            }

            if (shipBob != null)
            {
                shipBob.enabled = false;
            }
        }

        private void Update()
        {
            if (_stepsTaken < stepCount)
            {
                _timer += Time.deltaTime;
                if (_timer >= _secondsPerStep)
                {
                    _timer -= _secondsPerStep;
                    _stepsTaken++;
                    transform.localPosition = Vector3.Lerp(_startLocalPosition, _endLocalPosition, (float)_stepsTaken / stepCount);
                }
            }

            if (!_revealed && transform.localPosition.x < revealXThreshold)
            {
                _revealed = true;

                if (mainMenuCanvas != null)
                {
                    mainMenuCanvas.SetActive(true);
                }

                if (globalVolume != null)
                {
                    globalVolume.SetActive(true);
                }

                if (shipParticle != null)
                {
                    shipParticle.SetActive(true);
                }

                if (shipBob != null)
                {
                    shipBob.enabled = true;
                }
            }
        }
    }
}
