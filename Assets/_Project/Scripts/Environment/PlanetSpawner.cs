using System.Collections.Generic;
using Manatite.Core;
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
        [Tooltip("Plays a fade-to-black-and-back around a full field regeneration. Optional — " +
            "if unassigned, the field swaps instantly with no transition.")]
        [SerializeField] private SolarSystemTransitionUI transitionUI;

        /// <summary>Fired whenever a spawned planet's encounter is fully cleared. This
        /// script only reports its own domain (a planet became beaten) — it has no idea
        /// who, if anyone, cares (e.g. RunSaveController listening to autosave).</summary>
        public event System.Action OnPlanetBeaten;

        private PlayerInventory _playerInventory;

        /// <summary>Every planet spawned in the current batch (fresh field or loaded save),
        /// tracked so a full-field clear can be detected and the batch torn down for
        /// <see cref="RegenerateField"/>.</summary>
        private readonly List<PlanetState> _activePlanets = new List<PlanetState>();

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

            Vector3[] centers = SpawnRandomBatch(null);
            FacePlayerAtNearestPlanet(centers);
        }

        /// <summary>Scatters a fresh batch of <see cref="PlanetSpawnData.PlanetCount"/> planets,
        /// either across the configured field volume (<paramref name="centerOverride"/> null,
        /// initial scene spawn) or re-centered on the player (endless-field regeneration once
        /// every current planet is beaten — see <see cref="RegenerateField"/>). Returns the
        /// spawned planets' centers for optional ship-facing.</summary>
        private Vector3[] SpawnRandomBatch(Vector3? centerOverride)
        {
            PlanetData[] types = spawnData.PlanetTypes;
            if (types == null || types.Length == 0) return System.Array.Empty<Vector3>();

            Vector3 fieldCenter = centerOverride ?? spawnData.FieldCenter;

            var centers = new Vector3[spawnData.PlanetCount];
            var radii = new float[spawnData.PlanetCount];

            for (int i = 0; i < spawnData.PlanetCount; i++)
            {
                PlanetData type = types[Random.Range(0, types.Length)];
                float scale = Random.Range(type.MinScale, type.MaxScale);
                float radius = scale * 0.5f;

                Vector3 position = FindPosition(fieldCenter, radius, centers, radii, i);
                centers[i] = position;
                radii[i] = radius;

                SpawnPlanet(type, position, Random.rotation, scale, radius, i);
            }

            return centers;
        }

        /// <summary>Called once the last still-active planet in the current batch is beaten.
        /// Routes the actual swap through <see cref="transitionUI"/>'s fade-to-black-and-back
        /// (see <see cref="DoRegenerate"/>) so the teardown/respawn never happens on-screen;
        /// falls back to swapping instantly if no transition is assigned.</summary>
        private void RegenerateField()
        {
            if (spawnData == null) return;

            if (transitionUI != null)
                transitionUI.Play(DoRegenerate);
            else
                DoRegenerate();
        }

        /// <summary>Bumps the run's persistent difficulty tier (see <see cref="DifficultyScale"/>,
        /// read by <see cref="EnemySpawnZone"/>/<see cref="Enemies.EnemyController"/>/
        /// <see cref="Enemies.EnemyWeapon"/>), tears down the fully-looted batch, and scatters a
        /// fresh one centered on the player's current position so the run continues endlessly.</summary>
        private void DoRegenerate()
        {
            DifficultyScale.Tier++;

            foreach (PlanetState planet in _activePlanets)
                if (planet != null) Destroy(planet.gameObject);
            _activePlanets.Clear();

            Vector3? center = playerRigidbody != null ? (Vector3?)playerRigidbody.position : null;
            SpawnRandomBatch(center);
        }

        /// <summary>True once every planet in the current batch has been beaten — the trigger
        /// for <see cref="RegenerateField"/>.</summary>
        private bool AllPlanetsBeaten()
        {
            if (_activePlanets.Count == 0) return false;
            foreach (PlanetState planet in _activePlanets)
                if (planet != null && !planet.Beaten) return false;
            return true;
        }

        private void SpawnFromSave(List<PlanetSaveData> planets)
        {
            if (planets == null || planets.Count == 0) return;

            PlanetData[] types = spawnData != null ? spawnData.PlanetTypes : null;

            for (int i = 0; i < planets.Count; i++)
            {
                PlanetSaveData saved = planets[i];
                PlanetData type = FindTypeById(types, saved.typeId);
                if (type == null) continue;

                float radius = saved.scale * 0.5f;
                SpawnPlanet(type, saved.position, saved.rotation, saved.scale, radius, saved.planetId, saved.zoneRadius, saved.beaten, saved.hasShop);
            }
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

        private Vector3 FindPosition(Vector3 fieldCenter, float radius, Vector3[] centers, float[] radii, int placedCount)
        {
            Vector3 position = RandomPointInField(fieldCenter);
            for (int attempt = 0; attempt < spawnData.MaxPlacementAttempts; attempt++)
            {
                if (IsClear(position, radius, centers, radii, placedCount)) break;
                position = RandomPointInField(fieldCenter);
            }
            return position;
        }

        private bool IsClear(Vector3 position, float radius, Vector3[] centers, float[] radii, int placedCount)
        {
            for (int i = 0; i < placedCount; i++)
            {
                float minDistance = Mathf.Max(
                    (radius + radii[i]) * spawnData.MinSeparationMultiplier,
                    spawnData.MinAbsoluteSeparation);
                if (Vector3.SqrMagnitude(position - centers[i]) < minDistance * minDistance)
                    return false;
            }
            return true;
        }

        private Vector3 RandomPointInField(Vector3 center)
        {
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
            _activePlanets.Add(state);

            zone.OnEncounterCleared += () =>
            {
                state.Beaten = true;
                if (clearedRing != null)
                    clearedRing.Show();
                RollLoot(type.LootTable);
                RollShop(type.LootTable, instance.transform, radius);

                // Escalates every planet cleared (not just a full field), so future
                // encounters get harder gradually rather than in jumps every 3 planets.
                EncounterScaling.OnPlanetCleared();

                // Order matters: regenerate (and bump the difficulty tier) before the
                // OnPlanetBeaten autosave below, so the save captures the new field/tier
                // rather than the just-cleared one.
                if (AllPlanetsBeaten())
                    RegenerateField();

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
