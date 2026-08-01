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
        [Tooltip("Multiplies the spawned scale to size the enemy encounter trigger radius.")]
        [SerializeField] private float minZoneRadiusMultiplier = 1.3f;
        [SerializeField] private float maxZoneRadiusMultiplier = 1.8f;
        [SerializeField] private EnemySpawnData spawnTable;

        public Material Material => material;
        public float MinScale => minScale;
        public float MaxScale => maxScale;
        public float MinZoneRadiusMultiplier => minZoneRadiusMultiplier;
        public float MaxZoneRadiusMultiplier => maxZoneRadiusMultiplier;
        public EnemySpawnData SpawnTable => spawnTable;
    }
}
