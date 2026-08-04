using System.Collections.Generic;
using Manatite.Core;
using Manatite.Core.SaveSystem;
using Manatite.Data;
using Manatite.Enemies;
using UnityEngine;

namespace Manatite.Environment
{
    /// <summary>
    /// Scatters a fixed number of procedurally-typed, -sized, and -positioned planets
    /// across the playable field once at scene start. Each spawned planet carries its own
    /// randomized encounter trigger wired to its type's enemy spawn table. If a save is
    /// pending (<see cref="PendingLoad"/>), spawns the exact saved layout instead.
    /// </summary>
    public sealed class PlanetSpawner : MonoBehaviour
    {
        [SerializeField] private PlanetSpawnData spawnData;
        [SerializeField] private GameObject planetPrefab;

        /// <summary>Fired whenever a spawned planet's encounter is fully cleared. This
        /// script only reports its own domain (a planet became beaten) — it has no idea
        /// who, if anyone, cares (e.g. RunSaveController listening to autosave).</summary>
        public event System.Action OnPlanetBeaten;

        private void Start()
        {
            if (planetPrefab == null) return;

            if (PendingLoad.Data != null)
            {
                SpawnFromSave(PendingLoad.Data.planets);
                return;
            }

            if (spawnData == null) return;

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

                SpawnPlanet(type, position, Random.rotation, scale, radius, i);
            }

            FacePlayerAtNearestPlanet(centers);
        }

        private void SpawnFromSave(List<PlanetSaveData> planets)
        {
            if (planets == null || planets.Count == 0) return;

            PlanetData[] types = spawnData != null ? spawnData.PlanetTypes : null;
            var centers = new Vector3[planets.Count];

            for (int i = 0; i < planets.Count; i++)
            {
                PlanetSaveData saved = planets[i];
                PlanetData type = FindTypeById(types, saved.typeId);
                if (type == null) continue;

                float radius = saved.scale * 0.5f;
                SpawnPlanet(type, saved.position, saved.rotation, saved.scale, radius, saved.planetId, saved.zoneRadius, saved.beaten);
                centers[i] = saved.position;
            }

            FacePlayerAtNearestPlanet(centers);
        }

        private static PlanetData FindTypeById(PlanetData[] types, string typeId)
        {
            if (types == null) return null;
            foreach (PlanetData type in types)
                if (type != null && type.name == typeId)
                    return type;
            return null;
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

        private void SpawnPlanet(PlanetData type, Vector3 position, Quaternion rotation, float scale, float radius, int planetId, float zoneRadius = -1f, bool beaten = false)
        {
            GameObject instance = Instantiate(planetPrefab, position, rotation, transform);

            Transform visual = instance.transform.Find("Visual");
            visual.localScale = Vector3.one * scale;
            if (visual.TryGetComponent(out MeshRenderer renderer))
                renderer.sharedMaterial = type.Material;

            Transform zoneTransform = instance.transform.Find("SpawnZone");
            SphereCollider collider = zoneTransform.GetComponent<SphereCollider>();
            float resolvedZoneRadius = zoneRadius >= 0f
                ? zoneRadius
                : radius * Random.Range(type.MinZoneRadiusMultiplier, type.MaxZoneRadiusMultiplier);
            collider.radius = resolvedZoneRadius;

            EnemySpawnZone zone = zoneTransform.GetComponent<EnemySpawnZone>();
            zone.Configure(type.SpawnTable, radius);

            PlanetState state = instance.AddComponent<PlanetState>();
            state.Initialize(planetId, type.name, scale, resolvedZoneRadius);
            state.Beaten = beaten;

            zone.OnEncounterCleared += () =>
            {
                state.Beaten = true;
                OnPlanetBeaten?.Invoke();
            };
        }
    }
}
