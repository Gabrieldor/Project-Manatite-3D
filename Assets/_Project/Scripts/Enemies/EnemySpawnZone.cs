using System.Collections;
using Manatite.Combat;
using Manatite.Core;
using Manatite.Data;
using UnityEngine;

namespace Manatite.Enemies
{
    /// <summary>
    /// Encounter trigger: when the player enters, spawns enemies from
    /// <see cref="EnemySpawnData"/> around the player's current position (all at once
    /// or in timed waves) via <see cref="ObjectPooler"/>. Re-arms after a fixed cooldown
    /// once the encounter has finished spawning. Fires <see cref="OnEncounterCleared"/>
    /// once every enemy it spawned has been killed (fleeing past the leash distance
    /// doesn't count as a kill, so running from a wave never "clears" it).
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class EnemySpawnZone : MonoBehaviour
    {
        // Fixed Random ambiguity by using UnityEngine.Random
        [SerializeField] private EnemySpawnData spawnData;
        [Tooltip("Seconds after an encounter finishes spawning before this zone can trigger again.")]
        [SerializeField] private float rearmCooldown = 30f;

        public event System.Action OnEncounterCleared;

        private bool _armed = true;
        private Coroutine _spawnRoutine;
        private float _planetRadius;
        private int _remainingAlive;

        private void Awake()
        {
            GetComponent<SphereCollider>().isTrigger = true;
        }

        /// <summary>Assigns the roster this zone spawns from and the radius of the planet
        /// it belongs to, e.g. from a procedural planet spawner picking a type-specific
        /// table and randomized size at instantiation time. <paramref name="planetRadius"/>
        /// keeps spawned enemies clear of the planet's surface regardless of its size.</summary>
        public void Configure(EnemySpawnData data, float planetRadius)
        {
            spawnData = data;
            _planetRadius = planetRadius;
        }

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

            _remainingAlive = 0;
            foreach (EnemySpawnEntry entry in entries)
                _remainingAlive += entry.count;

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

            Vector3 offset = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(spawnData.MinSpawnRadius, spawnData.MaxSpawnRadius);
            Vector3 spawnPos = player.position + offset;

            // Clamp clear of the planet's surface: the offset above is relative to the
            // player, not the planet, so on a large planet it can otherwise land inside
            // the mesh. spawnData.MinSpawnRadius doubles as the required surface clearance.
            Vector3 fromPlanet = spawnPos - transform.position;
            float minDistance = _planetRadius + spawnData.MinSpawnRadius;
            if (fromPlanet.sqrMagnitude < minDistance * minDistance)
            {
                Vector3 dir = fromPlanet.sqrMagnitude > 0.0001f ? fromPlanet.normalized : Vector3.up;
                spawnPos = transform.position + dir * minDistance;
            }

            GameObject instance = ObjectPooler.Get(prefab, spawnPos, Quaternion.LookRotation(-offset.normalized, Vector3.up));
            if (instance.TryGetComponent(out EnemyController controller))
                controller.Initialize(player);

            if (instance.TryGetComponent(out Health health))
                TrackKill(health);
        }

        /// <summary>Subscribes with a self-unsubscribing handler so a pooled enemy instance
        /// reused later by a different zone/encounter can't leak a stale decrement into
        /// this zone's (already-finished) count.</summary>
        private void TrackKill(Health health)
        {
            System.Action onDied = null;
            onDied = () =>
            {
                health.OnDied -= onDied;
                _remainingAlive--;
                if (_remainingAlive <= 0)
                    OnEncounterCleared?.Invoke();
            };
            health.OnDied += onDied;
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
