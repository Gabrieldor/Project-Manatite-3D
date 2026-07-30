using Manatite.Combat;
using UnityEngine;

namespace Manatite.Player
{
    /// <summary>
    /// Placeholder game-over behavior: quits the moment the player's <see cref="Health"/>
    /// hits zero. Stand-in until a real death/respawn/game-over flow exists.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class PlayerDeath : MonoBehaviour
    {
        private Health _health;

        private void Awake() => _health = GetComponent<Health>();

        private void OnEnable() => _health.OnDied += HandleDeath;
        private void OnDisable() => _health.OnDied -= HandleDeath;

        private void HandleDeath()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
