using Manatite.Combat;
using Manatite.Core;
using Manatite.Data;
using UnityEngine;

namespace Manatite.Enemies
{
    /// <summary>
    /// Basic pursuit AI: steers and thrusts toward whatever target it was given,
    /// stopping just short of it. Releases itself back to the pool if it ever drifts
    /// past <see cref="EnemyData.LeashDistance"/>, or when its <see cref="Health"/>
    /// hits zero.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Health))]
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyData data;

        private Rigidbody _rb;
        private Health _health;
        private Transform _target;
        private Rigidbody _targetRigidbody;

        private bool _emerging;
        private Vector3 _emergeDirection;
        private Vector3 _planetCenter;
        private float _emergeExitDistance;

        public Transform Target => _target;
        public Vector3 TargetVelocity => _targetRigidbody != null ? _targetRigidbody.linearVelocity : Vector3.zero;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _health = GetComponent<Health>();
            ApplyData();
        }

        private void OnEnable()
        {
            _health.OnDied += HandleDeath;
            if (data != null) data.Changed += ApplyData;
        }

        private void OnDisable()
        {
            _health.OnDied -= HandleDeath;
            if (data != null) data.Changed -= ApplyData;
        }

        /// <summary>Applies the fields that are otherwise only read once (not every
        /// FixedUpdate) — re-run on <see cref="EnemyData.Changed"/> so Balance Configurator
        /// edits reach an already-spawned enemy in Play mode, not just the next spawn.</summary>
        private void ApplyData()
        {
            if (data == null) return;
            _health.SetMaxHealth(data.MaxHealth);
            transform.localScale = Vector3.one * data.Scale;
        }

        private void HandleDeath()
        {
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            GameObject deathVfxPrefab = data != null ? data.DeathVfxPrefab : null;

            ObjectPooler.Release(gameObject);

            if (deathVfxPrefab == null) return;
            GameObject vfx = ObjectPooler.Get(deathVfxPrefab, position, rotation);
            if (vfx.TryGetComponent(out IRadiusVfx effect))
                effect.Play(data.DeathVfxRadius);
        }

        /// <summary>Passing <paramref name="planetCenter"/> (e.g. from
        /// <see cref="EnemySpawnZone"/>) makes the enemy burst radially outward from that
        /// center at <see cref="EnemyData.EmergeSpeed"/> for <see cref="EnemyData.EmergeBurstDistance"/>
        /// beyond wherever it spawned, before normal pursuit takes over. Omit for spawns with
        /// no planet context (e.g. debug spawners) to skip straight to normal pursuit.</summary>
        public void Initialize(Transform target, Vector3? planetCenter = null)
        {
            _target = target;
            _targetRigidbody = target != null ? target.GetComponentInParent<Rigidbody>() : null;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            _emerging = planetCenter.HasValue;
            if (_emerging)
            {
                _planetCenter = planetCenter.Value;
                Vector3 fromPlanet = transform.position - _planetCenter;
                float spawnDistance = fromPlanet.magnitude;
                _emergeDirection = spawnDistance > 0.0001f ? fromPlanet / spawnDistance : transform.forward;
                _emergeExitDistance = spawnDistance + (data != null ? data.EmergeBurstDistance : 0f);
            }
        }

        private void FixedUpdate()
        {
            if (data == null) return;

            if (_emerging)
            {
                UpdateEmerge();
                return;
            }

            if (_target == null) return;

            Vector3 toTarget = _target.position - transform.position;
            float distance = toTarget.magnitude;

            if (distance > data.LeashDistance)
            {
                ObjectPooler.Release(gameObject);
                return;
            }

            Vector3 dir = toTarget / Mathf.Max(distance, 0.0001f);
            // Quaternion.LookRotation is numerically unstable when dir is nearly parallel to the
            // up reference; with free-flight 3D pursuit (enemies can end up almost directly above
            // or below the player), that instability caused aim to snap to a wrong heading for a
            // frame even on an otherwise-settled enemy. Fall back to a non-parallel reference axis.
            Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            Quaternion desiredRotation = Quaternion.LookRotation(dir, up);
            _rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, desiredRotation, data.TurnRate * Time.fixedDeltaTime));

            _rb.linearVelocity = distance > data.StopDistance ? transform.forward * data.MoveSpeed : Vector3.zero;
        }

        /// <summary>Straight radial burst away from the planet center, no steering, so the
        /// enemy can never curve back into the mesh mid-emerge. Ends once it has traveled
        /// <see cref="EnemyData.EmergeBurstDistance"/> beyond its own spawn point.</summary>
        private void UpdateEmerge()
        {
            Vector3 up = Mathf.Abs(Vector3.Dot(_emergeDirection, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            _rb.MoveRotation(Quaternion.LookRotation(_emergeDirection, up));
            _rb.linearVelocity = _emergeDirection * data.EmergeSpeed;

            if (Vector3.Distance(transform.position, _planetCenter) >= _emergeExitDistance)
                _emerging = false;
        }
    }
}
