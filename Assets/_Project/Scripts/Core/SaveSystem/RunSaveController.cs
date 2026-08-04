using Manatite.Combat;
using Manatite.Environment;
using UnityEngine;

namespace Manatite.Core.SaveSystem
{
    /// <summary>Gathers/applies this run's save state. One instance lives in the gameplay
    /// scene. Applies pending player state in Start() (not Awake) so this runs after every
    /// object's OnEnable — including Health.OnEnable, which resets current HP to max.</summary>
    public sealed class RunSaveController : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [Tooltip("Autosaves whenever this reports a planet was beaten.")]
        [SerializeField] private PlanetSpawner planetSpawner;

        private void OnEnable()
        {
            if (planetSpawner != null) planetSpawner.OnPlanetBeaten += Save;
        }

        private void OnDisable()
        {
            if (planetSpawner != null) planetSpawner.OnPlanetBeaten -= Save;
        }

        private void Start()
        {
            if (PendingLoad.Data != null)
                ApplyPlayerState(PendingLoad.Data.player);
        }

        private void ApplyPlayerState(PlayerSaveData data)
        {
            if (playerHealth == null || data == null) return;
            playerHealth.SetMaxHealth(data.maxHealth);
            playerHealth.SetCurrent(data.health);
        }

        public void Save()
        {
            var data = new SaveData();

            if (playerHealth != null)
            {
                data.player.health = playerHealth.Current;
                data.player.maxHealth = playerHealth.Max;
            }

            foreach (PlanetState state in FindObjectsByType<PlanetState>(FindObjectsSortMode.None))
            {
                data.planets.Add(new PlanetSaveData
                {
                    planetId = state.PlanetId,
                    typeId = state.TypeId,
                    position = state.transform.position,
                    rotation = state.transform.rotation,
                    scale = state.Scale,
                    zoneRadius = state.ZoneRadius,
                    beaten = state.Beaten
                });
            }

            SaveFile.Save(data);
        }
    }
}
