using UnityEngine;

namespace Manatite.Data
{
    /// <summary>
    /// Configures one procedural planet type (e.g. lava/ice/acid): its look, its size
    /// range, and the <see cref="EnemySpawnData"/> table its encounter zone spawns from.
    /// </summary>
    [CreateAssetMenu(fileName = "PlanetData_", menuName = "Manatite/Planet Data", order = 4)]
    public sealed class PlanetData : ScriptableObject
    {
        [Header("Look")]
        [SerializeField] private Material material;

        [Header("Size")]
        [SerializeField] private float minScale = 350f;
        [SerializeField] private float maxScale = 550f;

        [Header("Encounter")]
        [Tooltip("Extra distance beyond the planet's surface added to size the enemy encounter trigger radius.")]
        [SerializeField] private float minZoneClearance = 70f;
        [SerializeField] private float maxZoneClearance = 180f;
        [SerializeField] private EnemySpawnData spawnTable;

        [Header("Loot")]
        [Tooltip("Chips this planet's encounter can drop into the player's inventory when cleared. Optional.")]
        [SerializeField] private LootTableData lootTable;

        public Material Material => material;
        public float MinScale => minScale;
        public float MaxScale => maxScale;
        public float MinZoneClearance => minZoneClearance;
        public float MaxZoneClearance => maxZoneClearance;
        public EnemySpawnData SpawnTable => spawnTable;
        public LootTableData LootTable => lootTable;
    }
}
