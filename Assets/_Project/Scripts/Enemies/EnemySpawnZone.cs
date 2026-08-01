using System.Collections;
using Manatite.Core;
using Manatite.Data;
using UnityEngine;

namespace Manatite.Enemies
{
    /// <summary>
    /// Encounter trigger: when the player enters, spawns enemies from
    /// <see cref="EnemySpawnData"/> around the player's current position (all at once
    /// or in timed waves) via <see cref="ObjectPooler"/>. Re-arms after a fixed cooldown
    /// once the encounter has finished spawning.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class EnemySpawnZone : MonoBehaviour
    {
        [SerializeField] private EnemySpawnData spawnData;
        [Tooltip("Seconds after an encounter finishes spawning before this zone can trigger again.")]
        [SerializeField] private float rearmCooldown = 30f;

        private bool _armed = true;
        private Coroutine _spawnRoutine;

        private void Awake()
        {
            GetComponent<SphereCollider>().isTrigger = true;
        }

        /// <summary>Assigns the roster this zone spawns from, e.g. from a procedural
        /// planet spawner picking a type-specific table at instantiation time.</summary>
        public void Configure(EnemySpawnData data) => spawnData = data;

private void OnTriggerEnter(Collider other)
        {
            if (!_armed || spawnData == null) return;

            Rigidbody rb = other.attachedRigidbody;
            if (rb == null || !rb.CompareTag(GameLayers.PlayerTag)) return;

            // Enemies aim at this instead of the ship's raw pivot: the ship's hull isn't
            // centered on its own transform origin, so aiming at the bare pivot fires clean
            // over/under the actual hitbox. Falls back to the pivot if no CameraMount exists.
            Transform aimTarget = rb.transform.Find("CameraMount") ?? rb.transform;

            _armed = false;
            _spawnRoutine = StartCoroutine(SpawnEncounter(aimTarget));
        }

        private void OnDisable()
        {
            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }
        }

        private IEnumerator SpawnEncounter(Transform player)
        {
            EnemySpawnEntry[] entries = spawnData.Entries;
            if (entries == null || entries.Length == 0) yield break;

            if (spawnData.SpawnMode == EnemySpawnMode.AllAtOnce)
            {
                foreach (EnemySpawnEntry entry in entries)
                    for (int i = 0; i < entry.count; i++)
                        SpawnOne(entry.enemyPrefab, player);
            }
            else
            {
                var remaining = new int[entries.Length];
                for (int i = 0; i < remaining.Length; i++)
                    remaining[i] = entries[i].count;

                int totalRemaining = TotalOf(remaining);
                while (totalRemaining > 0)
                {
                    int toSpawn = Mathf.Min(spawnData.WaveSize, totalRemaining);
                    for (int spawned = 0; spawned < toSpawn; spawned++)
                    {
                        int entryIndex = NextEntryWithRemaining(remaining);
                        SpawnOne(entries[entryIndex].enemyPrefab, player);
                        remaining[entryIndex]--;
                    }

                    totalRemaining = TotalOf(remaining);
                    if (totalRemaining > 0)
                        yield return new WaitForSeconds(spawnData.TimeBetweenWaves);
                }
            }

            yield return new WaitForSeconds(rearmCooldown);
            _armed = true;
        }

        private void SpawnOne(GameObject prefab, Transform player)
        {
            if (prefab == null) return;

            Vector3 offset = Random.onUnitSphere * Random.Range(spawnData.MinSpawnRadius, spawnData.MaxSpawnRadius);
            Vector3 spawnPos = player.position + offset;

            GameObject instance = ObjectPooler.Get(prefab, spawnPos, Quaternion.LookRotation(-offset.normalized, Vector3.up));
            if (instance.TryGetComponent(out EnemyController controller))
                controller.Initialize(player);
        }

        private static int TotalOf(int[] remaining)
        {
            int total = 0;
            for (int i = 0; i < remaining.Length; i++) total += remaining[i];
            return total;
        }

        private static int NextEntryWithRemaining(int[] remaining)
        {
            for (int i = 0; i < remaining.Length; i++)
                if (remaining[i] > 0) return i;
            return 0;
        }
    }
}
