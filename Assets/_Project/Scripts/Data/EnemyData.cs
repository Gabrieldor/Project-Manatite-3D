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
        [Header("Health")]
        [SerializeField] private int maxHealth = 30;

        [Header("Pursuit")]
        [Tooltip("Linear speed toward the target (m/s).")]
        [SerializeField] private float moveSpeed = 12f;
        [Tooltip("Max turn rate while steering toward the target (deg/s).")]
        [SerializeField] private float turnRate = 90f;
        [Tooltip("Stops closing once within this distance of the target (m).")]
        [SerializeField] private float stopDistance = 15f;
        [Tooltip("Releases back to the pool once farther than this from the target (m). Safety valve until a proper despawn/death system exists.")]
        [SerializeField] private float leashDistance = 3000f;

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

        public int MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float TurnRate => turnRate;
        public float StopDistance => stopDistance;
        public float LeashDistance => leashDistance;
        public WeaponFireMode FireMode => fireMode;
        public float FireRate => fireRate;
        public float SingleFireDebounce => singleFireDebounce;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public int Damage => damage;
    }
}
