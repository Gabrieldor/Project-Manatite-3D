using Manatite.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Manatite.UI
{
    /// <summary>
    /// Music/SFX volume panel. Sliders read <see cref="AudioManager"/>'s current volume on
    /// open and write straight back to it on every drag; the underlying value is
    /// PlayerPrefs-backed, flushed to disk on <see cref="Close"/>.
    /// </summary>
    public sealed class SettingsMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [Tooltip("Hidden while this panel is open (e.g. the main button list) so the two don't overlap.")]
        [SerializeField] private GameObject hideWhileOpen;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [Tooltip("Selected on open so gamepad users have an immediate highlight.")]
        [SerializeField] private Selectable firstSelected;
        [Tooltip("Re-selected on close, e.g. the menu's Settings button.")]
        [SerializeField] private Selectable returnSelected;

        /// <summary>Lets a host menu (e.g. PauseMenu) check whether Settings should swallow a
        /// Cancel/Back input instead of closing itself.</summary>
        public bool IsOpen { get; private set; }

        public void Open()
        {
            IsOpen = true;
            panel.SetActive(true);
            if (hideWhileOpen != null) hideWhileOpen.SetActive(false);

            if (musicSlider != null) musicSlider.SetValueWithoutNotify(AudioManager.MusicVolume01);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(AudioManager.SfxVolume01);

            if (firstSelected != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
        }

        public void Close()
        {
            IsOpen = false;
            panel.SetActive(false);
            if (hideWhileOpen != null) hideWhileOpen.SetActive(true);
            PlayerPrefs.Save();

            if (returnSelected != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(returnSelected.gameObject);
        }

        public void OnMusicVolumeChanged(float value) => AudioManager.MusicVolume01 = value;

        public void OnSfxVolumeChanged(float value) => AudioManager.SfxVolume01 = value;
    }
}
