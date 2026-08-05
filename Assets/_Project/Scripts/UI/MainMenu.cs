using Manatite.Core.SaveSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Manatite.UI
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string newGameSceneName = "Outer Space";
        [Tooltip("Disabled unless a save file exists.")]
        [SerializeField] private Button loadGameButton;
        [Tooltip("Selected on menu open so gamepad users have an immediate highlight.")]
        [SerializeField] private Button firstSelected;

        /// <summary>Matches the alpha every always-enabled menu button (e.g. New Game) was
        /// authored with. This scene's disabled-looking buttons dim via a hand-painted Image
        /// alpha, not Selectable's color-tint transitions, so flipping interactable alone
        /// doesn't visually re-enable a button — the alpha has to move too.</summary>
        private const float EnabledButtonAlpha = 0.88f;

        private void Awake()
        {
            SetLoadGameInteractable(SaveFile.HasSave());
        }

        private void Start()
        {
            if (firstSelected != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
        }

        private void SetLoadGameInteractable(bool value)
        {
            if (loadGameButton == null) return;

            loadGameButton.interactable = value;
            if (!value) return;

            if (loadGameButton.image != null)
            {
                Color color = loadGameButton.image.color;
                color.a = EnabledButtonAlpha;
                loadGameButton.image.color = color;
            }

            var label = loadGameButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null)
            {
                Color labelColor = label.color;
                labelColor.a = 1f;
                label.color = labelColor;
            }
        }

        public void OnNewGame()
        {
            PendingLoad.Data = null;
            SceneManager.LoadScene(newGameSceneName);
        }

        public void OnLoadGame()
        {
            if (!SaveFile.TryLoad(out SaveData data)) return;

            PendingLoad.Data = data;
            SceneManager.LoadScene(newGameSceneName);
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
