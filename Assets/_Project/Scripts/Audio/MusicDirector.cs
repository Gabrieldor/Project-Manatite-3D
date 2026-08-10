using Manatite.Enemies;
using UnityEngine;

namespace Manatite.Audio
{
    /// <summary>
    /// Switches between the Idle and Battle music tracks based on how many
    /// <see cref="EnemySpawnZone"/> encounters are currently active in the scene. Plays Idle
    /// on start; one instance per gameplay scene.
    /// </summary>
    public sealed class MusicDirector : MonoBehaviour
    {
        private int _activeEncounters;

        private void Awake()
        {
            // Subscribed before any Start runs, so this also catches zones that
            // Environment.PlanetSpawner instantiates later in its own Start.
            EnemySpawnZone.AnyZoneSpawned += HandleZoneSpawned;

            foreach (EnemySpawnZone zone in FindObjectsByType<EnemySpawnZone>(FindObjectsSortMode.None))
                HandleZoneSpawned(zone);
        }

        private void OnDestroy()
        {
            EnemySpawnZone.AnyZoneSpawned -= HandleZoneSpawned;
        }

        private void HandleZoneSpawned(EnemySpawnZone zone)
        {
            zone.OnEncounterStarted += HandleEncounterStarted;
            zone.OnEncounterCleared += HandleEncounterCleared;
        }

        private void Start()
        {
            AudioManager.PlayMusic(MusicTrack.Idle);
        }

        private void HandleEncounterStarted()
        {
            _activeEncounters++;
            if (_activeEncounters == 1) AudioManager.PlayMusic(MusicTrack.Battle);
        }

        private void HandleEncounterCleared()
        {
            _activeEncounters = Mathf.Max(0, _activeEncounters - 1);
            if (_activeEncounters == 0) AudioManager.PlayMusic(MusicTrack.Idle);
        }
    }
}
