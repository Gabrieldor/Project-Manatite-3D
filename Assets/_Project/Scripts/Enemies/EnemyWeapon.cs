using Manatite.Combat;
using Manatite.Core;
using Manatite.Data;
using Manatite.Weapons;
using UnityEngine;

namespace Manatite.Enemies
{
    /// <summary>
    /// AI equivalent of <see cref="Weapon"/>: no input, just fires an <see cref="EnemyData"/>-driven
    /// cooldown while enabled and <see cref="canFire"/>. Cycles through <see cref="firePoints"/> one
    /// shot at a time, optionally gated by <see cref="EnemyData.RequireFirePointFacing"/> so a point
    /// only fires while it's actually oriented toward the target. Aims directly at
    /// <see cref="EnemyController"/>'s target rather than relying on the hull's own
    /// (turn-rate-limited) facing, with a random per-shot cone offset
    /// (<see cref="EnemyData.ScatterAngle"/>) as the only source of miss.
    /// </summary>
    public sealed class EnemyWeapon : MonoBehaviour
    {
        [SerializeField] private EnemyData data;
        [Tooltip("Where projectiles spawn/aim from. Cycles through all entries in order, one per shot. Defaults to this transform if unset.")]
        [SerializeField] private Transform[] firePoints;
        [Tooltip("Per-enemy toggle: uncheck to disable shooting without removing the component.")]
        [SerializeField] private bool canFire = true;

        private float _cooldownRemaining;
        private int _firePointIndex;
        private Collider _ownerCollider;
        private EnemyController _controller;

        private void Awake()
        {
            if (firePoints == null || firePoints.Length == 0) firePoints = new[] { transform };
            if (data == null)
                Debug.LogError($"{nameof(EnemyWeapon)} on '{name}' has no {nameof(EnemyData)} assigned.", this);
            _ownerCollider = GetComponentInParent<Collider>();
            _controller = GetComponentInParent<EnemyController>();
        }

        private void OnEnable() => _cooldownRemaining = 0f;

        private void Update()
        {
            if (!canFire || data == null) return;

            _cooldownRemaining -= Time.deltaTime;
            if (_cooldownRemaining <= 0f) TryFire();
        }

        private void TryFire()
        {
            Transform target = _controller != null ? _controller.Target : null;

            for (int i = 0; i < firePoints.Length; i++)
            {
                int index = (_firePointIndex + i) % firePoints.Length;
                Transform point = firePoints[index];
                if (!CanPointSeeTarget(point, target)) continue;

                Fire(point, target);
                _firePointIndex = (index + 1) % firePoints.Length;
                _cooldownRemaining = 1f / Mathf.Max(data.FireRate * DifficultyScale.Multiplier, 0.0001f);
                return;
            }
        }

        private bool CanPointSeeTarget(Transform point, Transform target)
        {
            if (!data.RequireFirePointFacing || target == null) return true;

            Vector3 toTarget = target.position - point.position;
            if (toTarget.sqrMagnitude < 0.0001f) return true;

            return Vector3.Angle(point.forward, toTarget) <= data.FirePointFovAngle;
        }

        private void Fire(Transform point, Transform target)
        {
            Quaternion rotation = ComputeAimRotation(point, target);
            int scaledDamage = Mathf.CeilToInt(data.Damage * DifficultyScale.Multiplier);
            Health attackerHealth = _controller != null ? _controller.Health : null;
            ProjectileLauncher.Fire(data.ProjectilePrefab, data.ProjectileSpeed, data.ProjectileLifetime, point.position, rotation, _ownerCollider, EffectProfile.BaseDamageOnly(scaledDamage), projectileScale: data.Scale, attackerHealth: attackerHealth);
        }

        private Quaternion ComputeAimRotation(Transform point, Transform target)
        {
            if (target == null) return point.rotation;

            Vector3 toTarget = target.position - point.position;
            if (toTarget.sqrMagnitude < 0.0001f) return point.rotation;

            Vector3 leadDir = PredictLeadDirection(toTarget, _controller.TargetVelocity, data.ProjectileSpeed);
            return Quaternion.LookRotation(ApplyScatter(leadDir, data.ScatterAngle));
        }

        // Exact constant-velocity intercept direction: solves |toTarget + targetVelocity * t| =
        // projectileSpeed * t for the smallest positive t (a firing-solution quadratic), rather
        // than approximating flight time from the current distance alone. The distance-only
        // approximation is only correct for purely tangential target motion — for a target
        // closing or opening range (the common case, since enemies chase the player) it over- or
        // under-leads, which is why shots with a radial velocity component used to miss.
        private static Vector3 PredictLeadDirection(Vector3 toTarget, Vector3 targetVelocity, float projectileSpeed)
        {
            float speedSqr = projectileSpeed * projectileSpeed;
            float a = Vector3.Dot(targetVelocity, targetVelocity) - speedSqr;
            float b = 2f * Vector3.Dot(toTarget, targetVelocity);
            float c = Vector3.Dot(toTarget, toTarget);

            float t = SolveInterceptTime(a, b, c);
            Vector3 predicted = toTarget + targetVelocity * t;
            return predicted.sqrMagnitude > 0.0001f ? predicted.normalized : toTarget.normalized;
        }

        // Smallest positive root of a*t^2 + b*t + c = 0. Falls back to t = 0 (aim at the
        // target's current position) whenever there's no real positive intercept, e.g. the
        // target is outrunning the projectile.
        private static float SolveInterceptTime(float a, float b, float c)
        {
            if (Mathf.Abs(a) < 0.0001f)
                return Mathf.Abs(b) > 0.0001f ? Mathf.Max(-c / b, 0f) : 0f;

            float discriminant = b * b - 4f * a * c;
            if (discriminant < 0f) return 0f;

            float sqrtDisc = Mathf.Sqrt(discriminant);
            float t1 = (-b + sqrtDisc) / (2f * a);
            float t2 = (-b - sqrtDisc) / (2f * a);

            if (t1 > 0f && t2 > 0f) return Mathf.Min(t1, t2);
            if (t1 > 0f) return t1;
            if (t2 > 0f) return t2;
            return 0f;
        }

        // Uniform-random direction within a cone of the given half-angle around forward,
        // sampled on a spherical cap so shots cluster correctly instead of biasing toward corners.
        private static Vector3 ApplyScatter(Vector3 forward, float halfAngleDegrees)
        {
            if (halfAngleDegrees <= 0f) return forward;

            float angleRad = halfAngleDegrees * Mathf.Deg2Rad;
            float z = Random.Range(Mathf.Cos(angleRad), 1f);
            float radius = Mathf.Sqrt(1f - z * z);
            float phi = Random.Range(0f, Mathf.PI * 2f);
            Vector3 local = new Vector3(radius * Mathf.Cos(phi), radius * Mathf.Sin(phi), z);

            return Quaternion.LookRotation(forward) * local;
        }
    }
}
