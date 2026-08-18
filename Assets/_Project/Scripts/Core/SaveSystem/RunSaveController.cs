using Manatite.Combat;
using Manatite.Core;
using Manatite.Environment;
using Manatite.Player;
using UnityEngine;

namespace Manatite.Core.SaveSystem
{
    /// <summary>Gathers/applies this run's save state. One instance lives in the gameplay
    /// scene. Applies pending player state in Start() (not Awake) so this runs after every
    /// object's OnEnable — including Health.OnEnable, which resets current HP to max.</summary>
    public sealed class RunSaveController : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private PlayerInventory playerInventory;
        [Tooltip("Autosaves whenever this reports a planet was beaten.")]
        [SerializeField] private PlanetSpawner planetSpawner;

        /// <summary>Resolved from <see cref="playerHealth"/>'s GameObject rather than a
        /// separate serialized field — the ship rig carries both on its root.</summary>
        private Rigidbody _playerRigidbody;

        private void Awake()
        {
            if (playerHealth != null) _playerRigidbody = playerHealth.GetComponent<Rigidbody>();
        }

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
            if (PendingLoad.Data == null)
            {
                DifficultyScale.Tier = 0;
                EncounterScaling.Reset();
                return;
            }

            DifficultyScale.Tier = PendingLoad.Data.difficultyTier;
            EncounterScaling.BasicBonus = PendingLoad.Data.basicEnemyBonus;
            EncounterScaling.MediumBonus = PendingLoad.Data.mediumEnemyBonus;
            EncounterScaling.LargeBonus = PendingLoad.Data.largeEnemyBonus;
            EncounterScaling.WaveSizeBonus = PendingLoad.Data.waveSizeBonus;
            ApplyPlayerState(PendingLoad.Data.player);
            if (playerInventory != null)
                playerInventory.RestoreState(PendingLoad.Data.inventory);
        }

        private void ApplyPlayerState(PlayerSaveData data)
        {
            if (playerHealth == null || data == null) return;
            playerHealth.SetMaxHealth(data.maxHealth);
            playerHealth.SetCurrent(data.health);

            if (_playerRigidbody != null)
            {
                _playerRigidbody.position = data.position;
                _playerRigidbody.rotation = data.rotation;
            }
        }

        public void Save()
        {
            var data = new SaveData();

            if (playerHealth != null)
            {
                data.player.health = playerHealth.Current;
                data.player.maxHealth = playerHealth.Max;
            }

            if (_playerRigidbody != null)
            {
                data.player.position = _playerRigidbody.position;
                data.player.rotation = _playerRigidbody.rotation;
            }

            if (playerInventory != null)
                data.inventory = playerInventory.CaptureState();

            data.difficultyTier = DifficultyScale.Tier;
            data.basicEnemyBonus = EncounterScaling.BasicBonus;
            data.mediumEnemyBonus = EncounterScaling.MediumBonus;
            data.largeEnemyBonus = EncounterScaling.LargeBonus;
            data.waveSizeBonus = EncounterScaling.WaveSizeBonus;

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
                    beaten = state.Beaten,
                    hasShop = state.GetComponentInChildren<ShopMarker>(true) != null
                });
            }

            SaveFile.Save(data);
        }
    }
}
