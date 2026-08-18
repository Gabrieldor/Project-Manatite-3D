using Manatite.Combat;
using Manatite.Core.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manatite.Player
{
    /// <summary>
    /// Game-over behavior: deletes the run's save (permadeath) and returns to the main
    /// menu the moment the player's <see cref="Health"/> hits zero. Stand-in until a real
    /// death/respawn/game-over flow (e.g. a death screen) exists.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class PlayerDeath : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "Main Menu";

        private Health _health;

        private void Awake() => _health = GetComponent<Health>();

        private void OnEnable() => _health.OnDied += HandleDeath;
        private void OnDisable() => _health.OnDied -= HandleDeath;

        private void HandleDeath()
        {
            SaveFile.DeleteSave();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
