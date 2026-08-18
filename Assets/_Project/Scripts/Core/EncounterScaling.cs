using UnityEngine;

namespace Manatite.Core
{
    /// <summary>
    /// Current run's cumulative extra-enemy escalation, bumped once per planet cleared
    /// (see <see cref="Manatite.Environment.PlanetSpawner"/> — every planet, not just a
    /// full field) and persisted via <see cref="Manatite.Core.SaveSystem.SaveData"/>. Read
    /// directly at spawn time by <see cref="Manatite.Enemies.EnemySpawnZone"/> rather than
    /// baked into the shared <c>EnemySpawnData</c> assets, so those ScriptableObjects are
    /// never mutated at runtime. Added on top of (not multiplied by) <see cref="DifficultyScale"/>.
    /// </summary>
    public static class EncounterScaling
    {
        private const int EnemiesPerClear = 5;
        private const int WaveSizeStepPerClear = 2;

        public static int BasicBonus { get; set; }
        public static int MediumBonus { get; set; }
        public static int LargeBonus { get; set; }
        public static int WaveSizeBonus { get; set; }

        public static void Reset()
        {
            BasicBonus = 0;
            MediumBonus = 0;
            LargeBonus = 0;
            WaveSizeBonus = 0;
        }

        /// <summary>Rolls <see cref="EnemiesPerClear"/> extra enemies, each independently
        /// landing on a random tier — a roll can end up all one tier or an even mix — and
        /// bumps the wave-size bonus by a flat step.</summary>
        public static void OnPlanetCleared()
        {
            for (int i = 0; i < EnemiesPerClear; i++)
            {
                switch (Random.Range(0, 3))
                {
                    case 0: BasicBonus++; break;
                    case 1: MediumBonus++; break;
                    default: LargeBonus++; break;
                }
            }

            WaveSizeBonus += WaveSizeStepPerClear;
        }
    }
}
