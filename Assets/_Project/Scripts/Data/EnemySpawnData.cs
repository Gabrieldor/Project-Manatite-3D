using System;
using UnityEngine;

namespace Manatite.Data
{
    public enum EnemySpawnMode
    {
        AllAtOnce,
        Waves
    }

    [Serializable]
    public struct EnemySpawnEntry
    {
        public GameObject enemyPrefab;
        public int count;
    }

    /// <summary>
    /// Configures what an <see cref="Manatite.Enemies.EnemySpawnZone"/> spawns: which
    /// enemies, how many, and whether they arrive all at once or in timed waves.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemySpawn_", menuName = "Manatite/Enemy Spawn Data", order = 3)]
    public sealed class EnemySpawnData : ScriptableObject
    {
        [Header("Roster")]
        [SerializeField] private EnemySpawnEntry[] entries;

        [Header("Timing")]
        [SerializeField] private EnemySpawnMode spawnMode = EnemySpawnMode.AllAtOnce;
        [Tooltip("Enemies spawned per wave. Ignored in AllAtOnce mode.")]
        [SerializeField] private int waveSize = 3;
        [Tooltip("Seconds between waves. Ignored in AllAtOnce mode.")]
        [SerializeField] private float timeBetweenWaves = 4f;

        [Header("Placement")]
        [Tooltip("Enemies spawn at a random point on a sphere shell around the player, at this range (m).")]
        [SerializeField] private float minSpawnRadius = 60f;
        [SerializeField] private float maxSpawnRadius = 120f;

        public EnemySpawnEntry[] Entries => entries;
        public EnemySpawnMode SpawnMode => spawnMode;
        public int WaveSize => waveSize;
        public float TimeBetweenWaves => timeBetweenWaves;
        public float MinSpawnRadius => minSpawnRadius;
        public float MaxSpawnRadius => maxSpawnRadius;
    }
}
