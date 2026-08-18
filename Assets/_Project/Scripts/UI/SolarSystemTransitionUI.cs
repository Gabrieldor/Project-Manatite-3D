using System;
using System.Collections;
using UnityEngine;

namespace Manatite.UI
{
    /// <summary>
    /// Full-screen fade-to-black-and-back used when the player's current planet field is
    /// fully cleared and a new one spawns in (see <see cref="Manatite.Environment.PlanetSpawner"/>).
    /// The screen is fully black by the time <see cref="Play"/>'s callback runs, so the old
    /// field's teardown and the new field's spawn never happen on-screen.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SolarSystemTransitionUI : MonoBehaviour
    {
        [SerializeField] private float fadeInDuration = 0.6f;
        [SerializeField] private float holdDuration = 1.2f;
        [SerializeField] private float fadeOutDuration = 0.6f;

        private CanvasGroup _canvasGroup;
        private Coroutine _routine;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            SetAlpha(0f);
        }

        /// <summary>Starts the fade-to-black-and-back. <paramref name="onBlackout"/> fires once
        /// the screen is fully black — do the actual field swap there so it's never visible.
        /// Ignored (with a warning) if a transition is already in progress.</summary>
        public void Play(Action onBlackout)
        {
            if (_routine != null)
            {
                Debug.LogWarning("SolarSystemTransitionUI.Play called while a transition was already running; ignoring.", this);
                return;
            }

            _routine = StartCoroutine(PlayRoutine(onBlackout));
        }

        private IEnumerator PlayRoutine(Action onBlackout)
        {
            _canvasGroup.blocksRaycasts = true;

            yield return Fade(0f, 1f, fadeInDuration);

            onBlackout?.Invoke();

            yield return new WaitForSeconds(holdDuration);

            yield return Fade(1f, 0f, fadeOutDuration);

            _canvasGroup.blocksRaycasts = false;
            _routine = null;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                SetAlpha(to);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }

            SetAlpha(to);
        }

        private void SetAlpha(float alpha) => _canvasGroup.alpha = alpha;
    }
}
