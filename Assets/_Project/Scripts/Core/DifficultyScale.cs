using UnityEngine;

namespace Manatite.Core
{
    /// <summary>
    /// Current run's difficulty tier, bumped once per full field clear (see
    /// <see cref="Manatite.Environment.PlanetSpawner"/>) and persisted via
    /// <see cref="Manatite.Core.SaveSystem.SaveData.difficultyTier"/>. Read directly at
    /// spawn/fire time by <see cref="Manatite.Enemies.EnemySpawnZone"/>,
    /// <see cref="Manatite.Enemies.EnemyController"/>, and <see cref="Manatite.Enemies.EnemyWeapon"/>
    /// rather than baked into the shared <c>EnemyData</c>/<c>EnemySpawnData</c> assets, so those
    /// ScriptableObjects are never mutated at runtime.
    /// </summary>
    public static class DifficultyScale
    {
        public static int Tier { get; set; }

        /// <summary>Compounding +10% per tier, recomputed from base each read rather than
        /// chained roundings, so scaling never drifts across cycles.</summary>
        public static float Multiplier => Mathf.Pow(1.1f, Tier);
    }
}
