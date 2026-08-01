using Manatite.Core;
using Manatite.Data;
using Manatite.Enemies;
using UnityEngine;

namespace Manatite.Environment
{
    /// <summary>
    /// Scatters a fixed number of procedurally-typed, -sized, and -positioned planets
    /// across the playable field once at scene start. Each spawned planet carries its own
    /// randomized encounter trigger wired to its type's enemy spawn table.
    /// </summary>
    public sealed class PlanetSpawner : MonoBehaviour
    {
        [SerializeField] private PlanetSpawnData spawnData;
        [SerializeField] private GameObject planetPrefab;

        private void Start()
        {
            if (spawnData == null || planetPrefab == null) return;

            PlanetData[] types = spawnData.PlanetTypes;
            if (types == null || types.Length == 0) return;

            var centers = new Vector3[spawnData.PlanetCount];
            var radii = new float[spawnData.PlanetCount];

            for (int i = 0; i < spawnData.PlanetCount; i++)
            {
                PlanetData type = types[Random.Range(0, types.Length)];
                float scale = Random.Range(type.MinScale, type.MaxScale);
                float radius = scale * 0.5f;

                Vector3 position = FindPosition(radius, centers, radii, i);
                centers[i] = position;
                radii[i] = radius;

                SpawnPlanet(type, position, scale, radius);
            }

            FacePlayerAtNearestPlanet(centers);
        }

        /// <summary>Points the ship's nose (and its rigidly-mounted cockpit camera) at
        /// whichever planet spawned closest, so the player isn't staring at empty space
        /// the first frame they gain control.</summary>
        private void FacePlayerAtNearestPlanet(Vector3[] centers)
        {
            if (centers.Length == 0) return;

            GameObject player = GameObject.FindGameObjectWithTag(GameLayers.PlayerTag);
            if (player == null || !player.TryGetComponent(out Rigidbody rb)) return;

            Vector3 nearest = centers[0];
            float nearestSqrDistance = float.MaxValue;
            foreach (Vector3 center in centers)
            {
                float sqrDistance = Vector3.SqrMagnitude(center - rb.position);
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = center;
                }
            }

            Vector3 dir = (nearest - rb.position).normalized;
            Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            rb.rotation = Quaternion.LookRotation(dir, up);
        }

        private Vector3 FindPosition(float radius, Vector3[] centers, float[] radii, int placedCount)
        {
            Vector3 position = RandomPointInField();
            for (int attempt = 0; attempt < spawnData.MaxPlacementAttempts; attempt++)
            {
                if (IsClear(position, radius, centers, radii, placedCount)) break;
                position = RandomPointInField();
            }
            return position;
        }

        private bool IsClear(Vector3 position, float radius, Vector3[] centers, float[] radii, int placedCount)
        {
            for (int i = 0; i < placedCount; i++)
            {
                float minDistance = (radius + radii[i]) * spawnData.MinSeparationMultiplier;
                if (Vector3.SqrMagnitude(position - centers[i]) < minDistance * minDistance)
                    return false;
            }
            return true;
        }

        private Vector3 RandomPointInField()
        {
            Vector3 center = spawnData.FieldCenter;
            Vector3 extents = spawnData.FieldHalfExtents;
            return new Vector3(
                center.x + Random.Range(-extents.x, extents.x),
                center.y + Random.Range(-extents.y, extents.y),
                center.z + Random.Range(-extents.z, extents.z));
        }

        private void SpawnPlanet(PlanetData type, Vector3 position, float scale, float radius)
        {
            GameObject instance = Instantiate(planetPrefab, position, Random.rotation, transform);

            Transform visual = instance.transform.Find("Visual");
            visual.localScale = Vector3.one * scale;
            if (visual.TryGetComponent(out MeshRenderer renderer))
                renderer.sharedMaterial = type.Material;

            Transform zone = instance.transform.Find("SpawnZone");
            zone.GetComponent<SphereCollider>().radius = radius * Random.Range(type.MinZoneRadiusMultiplier, type.MaxZoneRadiusMultiplier);
            zone.GetComponent<EnemySpawnZone>().Configure(type.SpawnTable);
        }
    }
}
