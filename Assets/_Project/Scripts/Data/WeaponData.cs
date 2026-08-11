using UnityEngine;

namespace Manatite.Data
{
    public enum WeaponFireMode
    {
        Auto,
        Single
    }

    /// <summary>
    /// Tunables for a projectile weapon. See <see cref="Manatite.Weapons.Weapon"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_", menuName = "Manatite/Weapon Data", order = 1)]
    public sealed class WeaponData : ScriptableObject
    {
        [Header("Fire behavior")]
        [Tooltip("Auto: fires continuously while held. Single: one shot per press.")]
        [SerializeField] private WeaponFireMode fireMode = WeaponFireMode.Auto;
        [Tooltip("Shots per second. Also the minimum interval between shots in Single mode.")]
        [SerializeField] private float fireRate = 6f;
        [Tooltip("Single mode only: seconds the Fire button must read released before a new press counts as a new shot. Filters out button/trigger contact bounce so one physical click can't register as several.")]
        [SerializeField] private float singleFireDebounce = 0.05f;

        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 400f;
        [Tooltip("Seconds before an unhit projectile despawns on its own.")]
        [SerializeField] private float projectileLifetime = 3f;
        [SerializeField] private int damage = 1;
        [Tooltip("Spawned at the impact point whenever an equipped chip grants a splash radius > 0.")]
        [SerializeField] private GameObject splashVfxPrefab;
        [Tooltip("Spawned at the impact point on every hit this weapon lands (no chip required).")]
        [SerializeField] private GameObject hitMarkerPrefab;
        [Tooltip("Spawned between each pair of chained targets whenever an equipped chip grants a lightning chain count > 0.")]
        [SerializeField] private GameObject lightningVfxPrefab;

        public WeaponFireMode FireMode => fireMode;
        public float FireRate => fireRate;
        public float SingleFireDebounce => singleFireDebounce;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public int Damage => damage;
        public GameObject SplashVfxPrefab => splashVfxPrefab;
        public GameObject HitMarkerPrefab => hitMarkerPrefab;
        public GameObject LightningVfxPrefab => lightningVfxPrefab;
    }
}
