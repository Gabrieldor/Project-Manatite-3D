using System;
using UnityEngine;

namespace Manatite.Data
{
    /// <summary>
    /// Complete tunable stat block for one enemy type (health, pursuit, and weapon
    /// behavior together). See <see cref="Manatite.Enemies.EnemyController"/> and
    /// <see cref="Manatite.Enemies.EnemyWeapon"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData_", menuName = "Manatite/Enemy Data", order = 2)]
    public sealed class EnemyData : ScriptableObject
    {
        /// <summary>Raised whenever a field is edited (e.g. by the Balance Configurator), so
        /// live instances can re-apply values that are otherwise only read once at spawn.</summary>
        public event Action Changed;
        private void OnValidate() => Changed?.Invoke();


        [Header("Health")]
        [SerializeField] private int maxHealth = 30;

        [Header("Size")]
        [Tooltip("Uniform scale applied to the enemy root at spawn. Collider and fire point " +
                 "are defined in root-local space, so they scale with this automatically.")]
        [SerializeField] private float scale = 1f;

        [Header("Pursuit")]
        [Tooltip("Linear speed toward the target (m/s).")]
        [SerializeField] private float moveSpeed = 12f;
        [Tooltip("Max turn rate while steering toward the target (deg/s).")]
        [SerializeField] private float turnRate = 90f;
        [Tooltip("Stops closing once within this distance of the target (m).")]
        [SerializeField] private float stopDistance = 15f;
        [Tooltip("Releases back to the pool once farther than this from the target (m). Safety valve until a proper despawn/death system exists.")]
        [SerializeField] private float leashDistance = 3000f;

        [Header("Emerge")]
        [Tooltip("Straight-line speed (m/s) while bursting away from the planet right after spawn, before normal pursuit takes over.")]
        [SerializeField] private float emergeSpeed = 35f;
        [Tooltip("Distance (m) the burst travels outward beyond its own spawn point before handing off to normal pursuit steering.")]
        [SerializeField] private float emergeBurstDistance = 150f;

        [Header("Death")]
        [Tooltip("Pooled VFX spawned at the enemy's position when it dies. Optional.")]
        [SerializeField] private GameObject deathVfxPrefab;
        [Tooltip("World-space radius passed to the death VFX's IRadiusVfx.Play() — tune per " +
                 "enemy type to read clearly against that type's actual (post-Scale) hull size.")]
        [SerializeField] private float deathVfxRadius = 20f;

        [Header("Weapon")]
        [SerializeField] private WeaponFireMode fireMode = WeaponFireMode.Auto;
        [Tooltip("Shots per second.")]
        [SerializeField] private float fireRate = 1f;
        [SerializeField] private float singleFireDebounce = 0.05f;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 250f;
        [Tooltip("Seconds before an unhit projectile despawns on its own.")]
        [SerializeField] private float projectileLifetime = 3f;
        [SerializeField] private int damage = 1;
        [Tooltip("Half-angle (degrees) of the random aim cone applied to each shot. 0 = perfect aim.")]
        [SerializeField] private float scatterAngle = 0f;
        [Tooltip("If enabled, a fire point can only fire while the target is within Fire Point Fov Angle of the point's own forward direction. If disabled, fire points shoot regardless of which way they face.")]
        [SerializeField] private bool requireFirePointFacing = false;
        [Tooltip("Half-angle (degrees) of the cone, centered on each fire point's forward direction, within which it's allowed to fire. Only used when Require Fire Point Facing is enabled.")]
        [SerializeField] private float firePointFovAngle = 90f;

        public int MaxHealth => maxHealth;
        public float Scale => scale;
        public GameObject DeathVfxPrefab => deathVfxPrefab;
        public float DeathVfxRadius => deathVfxRadius;
        public float MoveSpeed => moveSpeed;
        public float TurnRate => turnRate;
        public float StopDistance => stopDistance;
        public float LeashDistance => leashDistance;
        public float EmergeSpeed => emergeSpeed;
        public float EmergeBurstDistance => emergeBurstDistance;
        public WeaponFireMode FireMode => fireMode;
        public float FireRate => fireRate;
        public float SingleFireDebounce => singleFireDebounce;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public int Damage => damage;
        public float ScatterAngle => scatterAngle;
        public bool RequireFirePointFacing => requireFirePointFacing;
        public float FirePointFovAngle => firePointFovAngle;
    }
}
