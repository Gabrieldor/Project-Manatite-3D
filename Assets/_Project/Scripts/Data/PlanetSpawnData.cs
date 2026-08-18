using UnityEngine;

namespace Manatite.Data
{
    /// <summary>
    /// Configures the <see cref="Manatite.Environment.PlanetSpawner"/>: which planet types
    /// it can pick from, how many to place, and the field volume to scatter them across.
    /// </summary>
    [CreateAssetMenu(fileName = "PlanetSpawn_", menuName = "Manatite/Planet Spawn Data", order = 5)]
    public sealed class PlanetSpawnData : ScriptableObject
    {
        [Header("Roster")]
        [Tooltip("Each planet independently picks a random entry — duplicates allowed.")]
        [SerializeField] private PlanetData[] planetTypes;
        [SerializeField] private int planetCount = 3;

        [Header("Field volume")]
        [SerializeField] private Vector3 fieldCenter = new Vector3(275f, 0f, 137.5f);
        [SerializeField] private Vector3 fieldHalfExtents = new Vector3(4675f, 3300f, 5087.5f);

        [Header("Placement")]
        [Tooltip("Minimum gap between planet surfaces, as a multiple of the sum of their radii.")]
        [SerializeField] private float minSeparationMultiplier = 1.2f;
        [Tooltip("Minimum center-to-center distance between any two planets, regardless of their " +
            "radii. Takes precedence over MinSeparationMultiplier whenever it would require a larger gap.")]
        [SerializeField] private float minAbsoluteSeparation = 1500f;
        [SerializeField] private int maxPlacementAttempts = 20;

        public PlanetData[] PlanetTypes => planetTypes;
        public int PlanetCount => planetCount;
        public Vector3 FieldCenter => fieldCenter;
        public Vector3 FieldHalfExtents => fieldHalfExtents;
        public float MinSeparationMultiplier => minSeparationMultiplier;
        public float MinAbsoluteSeparation => minAbsoluteSeparation;
        public int MaxPlacementAttempts => maxPlacementAttempts;
    }
}
