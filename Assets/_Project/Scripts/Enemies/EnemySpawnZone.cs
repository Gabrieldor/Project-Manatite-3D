using System.Collections;
using Manatite.Combat;
using Manatite.Core;
using Manatite.Data;
using UnityEngine;

namespace Manatite.Enemies
{
    /// <summary>
    /// Encounter trigger: when the player enters, spawns enemies from
    /// <see cref="EnemySpawnData"/> in a cluster behind the planet's far side from the
    /// player (all at once, or in waves that each wait for the previous wave to be fully
    /// killed before the next launches) via <see cref="ObjectPooler"/> — each enemy then
    /// bursts outward from there (see <see cref="EnemyController"/>'s emerge state) before
    /// settling into normal pursuit. Re-arms after a fixed cooldown
    /// once the encounter has finished spawning — unless it's been fully cleared, in which
    /// case the zone is done for good and never spawns again. Fires
    /// <see cref="OnEncounterCleared"/> once every enemy it spawned has been killed
    /// (fleeing past the leash distance doesn't count as a kill, so running from a wave
    /// never "clears" it, and the zone can still re-arm and be re-triggered).
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class EnemySpawnZone : MonoBehaviour
    {
        // Fixed Random ambiguity by using UnityEngine.Random
        [SerializeField] private EnemySpawnData spawnData;
        [Tooltip("Seconds after an encounter finishes spawning before this zone can trigger again.")]
        [SerializeField] private float rearmCooldown = 30f;
        [Tooltip("Half-angle (degrees) of the cone, centered on the planet's far side from the player, that enemies spawn within — keeps them clustered behind the planet without stacking exactly on one point.")]
        [SerializeField] private float emergeConeHalfAngle = 35f;

        public event System.Action OnEncounterStarted;
        public event System.Action OnEncounterCleared;

        /// <summary>Fired the instant any zone (procedurally spawned or scene-placed) exists,
        /// so listeners (e.g. <see cref="Manatite.Audio.MusicDirector"/>) don't have to find
        /// zones at their own Awake — <see cref="Environment.PlanetSpawner"/> instantiates
        /// planets (and their zones) in Start, which always runs after every Awake, so a
        /// one-time FindObjectsByType at Awake would see zero zones.</summary>
        public static event System.Action<EnemySpawnZone> AnyZoneSpawned;

        private bool _armed = true;
        private bool _encounterCleared;
        private Coroutine _spawnRoutine;
        private float _planetRadius;
        private int _remainingAlive;
        private int _currentWaveAlive;

        private void Awake()
        {
            GetComponent<SphereCollider>().isTrigger = true;
            AnyZoneSpawned?.Invoke(this);
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
            if (!_armed || _encounterCleared || spawnData == null) return;

            Rigidbody rb = other.attachedRigidbody;
            if (rb == null || !rb.CompareTag(GameLayers.PlayerTag)) return;

            // Enemies aim at this instead of the ship's raw pivot: the ship's hull isn't
            // centered on its own transform origin, so aiming at the bare pivot fires clean
            // over/under the actual hitbox. Falls back to the pivot if no CameraMount exists.
            Transform aimTarget = rb.transform.Find("CameraMount") ?? rb.transform;

            _armed = false;
            OnEncounterStarted?.Invoke();
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

            // Rounded up per entry rather than once against a summed total, so a 1-count
            // entry still gets its own +1 at tier 1 instead of being lost to rounding.
            var scaledCounts = new int[entries.Length];
            for (int i = 0; i < entries.Length; i++)
                scaledCounts[i] = Mathf.CeilToInt(entries[i].count * DifficultyScale.Multiplier);

            _remainingAlive = 0;
            foreach (int count in scaledCounts)
                _remainingAlive += count;

            if (spawnData.SpawnMode == EnemySpawnMode.AllAtOnce)
            {
                // Single batch, so one read of the player's position covers the whole spawn.
                Vector3 backDirection = BackDirectionFrom(player.position);
                for (int i = 0; i < entries.Length; i++)
                    for (int j = 0; j < scaledCounts[i]; j++)
                        SpawnOne(entries[i].enemyPrefab, player, backDirection);
            }
            else
            {
                var remaining = (int[])scaledCounts.Clone();

                int totalRemaining = TotalOf(remaining);
                while (totalRemaining > 0)
                {
                    // Re-measured every wave (not just once for the whole encounter) so a
                    // later wave still emerges from behind the planet as seen from wherever
                    // the player has actually flown to since the encounter started.
                    Vector3 backDirection = BackDirectionFrom(player.position);
                    _currentWaveAlive = 0;
                    int toSpawn = Mathf.Min(spawnData.WaveSize, totalRemaining);
                    for (int spawned = 0; spawned < toSpawn; spawned++)
                    {
                        int entryIndex = NextEntryWithRemaining(remaining);
                        SpawnOne(entries[entryIndex].enemyPrefab, player, backDirection);
                        remaining[entryIndex]--;
                    }

                    totalRemaining = TotalOf(remaining);
                    if (totalRemaining > 0)
                    {
                        // Next wave only launches once every enemy from this one is dead,
                        // not on a fixed timer — TimeBetweenWaves is just a breather after that.
                        yield return new WaitUntil(() => _currentWaveAlive <= 0);
                        yield return new WaitForSeconds(spawnData.TimeBetweenWaves);
                    }
                }
            }

            yield return new WaitForSeconds(rearmCooldown);
            _armed = true;
        }

        /// <summary>Direction from the planet's center pointing away from the player —
        /// i.e. toward the planet's far side as currently seen from the player.</summary>
        private Vector3 BackDirectionFrom(Vector3 playerPosition)
        {
            Vector3 fromPlayer = transform.position - playerPosition;
            return fromPlayer.sqrMagnitude > 0.0001f ? fromPlayer.normalized : Vector3.up;
        }

        private void SpawnOne(GameObject prefab, Transform player, Vector3 backDirection)
        {
            if (prefab == null) return;

            // Spawn within a cone around the planet's far side from the player, clear of
            // its surface, rather than scattered around the player — enemies then burst
            // outward from there (EnemyController's emerge state) instead of popping in
            // already next to the player.
            Vector3 dir = RandomDirectionInCone(backDirection, emergeConeHalfAngle);
            float distance = _planetRadius + UnityEngine.Random.Range(spawnData.MinSpawnRadius, spawnData.MaxSpawnRadius);
            Vector3 spawnPos = transform.position + dir * distance;

            Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            GameObject instance = ObjectPooler.Get(prefab, spawnPos, Quaternion.LookRotation(dir, up));
            if (instance.TryGetComponent(out EnemyController controller))
                controller.Initialize(player, transform.position);

            if (instance.TryGetComponent(out Health health))
                TrackKill(health);
        }

        /// <summary>Uniformly samples a direction within <paramref name="halfAngleDegrees"/>
        /// of <paramref name="coneDirection"/>, used to spread spawns around the planet's
        /// far side without stacking them on one exact point.</summary>
        private static Vector3 RandomDirectionInCone(Vector3 coneDirection, float halfAngleDegrees)
        {
            Vector3 up = Mathf.Abs(Vector3.Dot(coneDirection, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            Vector3 right = Vector3.Cross(up, coneDirection).normalized;
            Vector3 forward = Vector3.Cross(coneDirection, right).normalized;

            float polar = UnityEngine.Random.Range(0f, halfAngleDegrees) * Mathf.Deg2Rad;
            float azimuth = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;

            Vector3 spread = Mathf.Cos(azimuth) * right + Mathf.Sin(azimuth) * forward;
            return (Mathf.Cos(polar) * coneDirection + Mathf.Sin(polar) * spread).normalized;
        }

        /// <summary>Subscribes with a self-unsubscribing handler so a pooled enemy instance
        /// reused later by a different zone/encounter can't leak a stale decrement into
        /// this zone's (already-finished) count.</summary>
        private void TrackKill(Health health)
        {
            _currentWaveAlive++;
            System.Action onDied = null;
            onDied = () =>
            {
                health.OnDied -= onDied;
                _remainingAlive--;
                _currentWaveAlive--;
                if (_remainingAlive <= 0)
                {
                    _encounterCleared = true;
                    OnEncounterCleared?.Invoke();
                }
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
