using System.Collections.Generic;
using Manatite.Core.SaveSystem;
using Manatite.Data;
using Manatite.Enemies;
using Manatite.Player;
using Manatite.UI;
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
        [Tooltip("Instantiated above a planet on clear when its loot table's shop chance rolls " +
            "successfully. Also used by ShopTestSpawner for debug spawning.")]
        [SerializeField] private GameObject shopMarkerPrefab;
        [Tooltip("The scene's shop panel, passed to every spawned marker so it knows what to open.")]
        [SerializeField] private ShopPanel shopPanel;
        [Tooltip("World-unit clearance above the planet's surface the shop marker spawns at.")]
        [SerializeField] private float shopMarkerHeightOffset = 80f;
        [Tooltip("Assigned directly to the ship rig rather than resolved via tag lookup — " +
            "the ship's mesh children also carry the Player tag, which makes " +
            "GameObject.FindGameObjectWithTag ambiguous.")]
        [SerializeField] private Rigidbody playerRigidbody;

        /// <summary>Fired whenever a spawned planet's encounter is fully cleared. This
        /// script only reports its own domain (a planet became beaten) — it has no idea
        /// who, if anyone, cares (e.g. RunSaveController listening to autosave).</summary>
        public event System.Action OnPlanetBeaten;

        private PlayerInventory _playerInventory;

        private void Start()
        {
            if (planetPrefab == null) return;

            if (playerRigidbody != null) _playerInventory = playerRigidbody.GetComponent<PlayerInventory>();

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
                SpawnPlanet(type, saved.position, saved.rotation, saved.scale, radius, saved.planetId, saved.zoneRadius, saved.beaten, saved.hasShop);
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
            if (centers.Length == 0 || playerRigidbody == null) return;

            Rigidbody rb = playerRigidbody;
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

        private void SpawnPlanet(PlanetData type, Vector3 position, Quaternion rotation, float scale, float radius, int planetId, float zoneRadius = -1f, bool beaten = false, bool hasShop = false)
        {
            GameObject instance = Instantiate(planetPrefab, position, rotation, transform);

            Transform visual = instance.transform.Find("Visual");
            visual.localScale = Vector3.one * scale;
            if (visual.TryGetComponent(out MeshRenderer renderer))
                renderer.sharedMaterial = type.Material;

            PlanetClearedRing clearedRing = visual.GetComponentInChildren<PlanetClearedRing>(true);

            Transform zoneTransform = instance.transform.Find("SpawnZone");
            SphereCollider collider = zoneTransform.GetComponent<SphereCollider>();
            float resolvedZoneRadius = zoneRadius >= 0f
                ? zoneRadius
                : radius + Random.Range(type.MinZoneClearance, type.MaxZoneClearance);
            collider.radius = resolvedZoneRadius;

            EnemySpawnZone zone = zoneTransform.GetComponent<EnemySpawnZone>();
            zone.Configure(type.SpawnTable, radius);

            PlanetState state = instance.AddComponent<PlanetState>();
            state.Initialize(planetId, type.name, scale, resolvedZoneRadius);
            state.Beaten = beaten;
            if (beaten && clearedRing != null)
                clearedRing.Show();
            if (hasShop)
                SpawnShopMarker(instance.transform, radius);

            zone.OnEncounterCleared += () =>
            {
                state.Beaten = true;
                if (clearedRing != null)
                    clearedRing.Show();
                RollLoot(type.LootTable);
                RollShop(type.LootTable, instance.transform, radius);
                OnPlanetBeaten?.Invoke();
            };
        }

        /// <summary>Rolls the loot table's independent shop chance and, on success, spawns
        /// <see cref="shopMarkerPrefab"/> above the planet's surface.</summary>
        private void RollShop(LootTableData lootTable, Transform planetRoot, float radius)
        {
            if (lootTable == null) return;
            if (Random.value > lootTable.ShopChance) return;

            SpawnShopMarker(planetRoot, radius);
        }

        /// <summary>Instantiates <see cref="shopMarkerPrefab"/> above the planet's surface,
        /// unconditionally — used both by a fresh <see cref="RollShop"/> success and by
        /// restoring a planet whose save recorded an already-rolled shop marker.</summary>
        private void SpawnShopMarker(Transform planetRoot, float radius)
        {
            if (shopMarkerPrefab == null) return;

            GameObject marker = Instantiate(shopMarkerPrefab, planetRoot, false);
            marker.transform.localPosition = Vector3.up * (radius + shopMarkerHeightOffset);
            if (marker.TryGetComponent(out ShopMarker shopMarker))
                shopMarker.Initialize(shopPanel);
        }

        /// <summary>Rolls each entry's drop chance independently and hands successful
        /// drops to the player's inventory. A full inventory just means that item is
        /// skipped — no other effect.</summary>
        private void RollLoot(LootTableData lootTable)
        {
            if (lootTable == null || _playerInventory == null) return;

            LootEntry[] entries = lootTable.Entries;
            if (entries == null) return;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].item == null) continue;
                if (Random.value <= entries[i].dropChance)
                    _playerInventory.TryAddItem(entries[i].item);
            }
        }
    }
}
