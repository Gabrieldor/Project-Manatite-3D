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

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _health = GetComponent<Health>();
            if (data != null) _health.SetMaxHealth(data.MaxHealth);
        }

        private void OnEnable() => _health.OnDied += HandleDeath;
        private void OnDisable() => _health.OnDied -= HandleDeath;

        private void HandleDeath() => ObjectPooler.Release(gameObject);

        public void Initialize(Transform target)
        {
            _target = target;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (data == null || _target == null) return;

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
    }
}
