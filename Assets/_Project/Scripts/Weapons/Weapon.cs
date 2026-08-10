using Manatite.Data;
using Manatite.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manatite.Weapons
{
    /// <summary>
    /// Primary fire: spawns pooled <see cref="Projectile"/> instances from
    /// <see cref="firePoint"/> while the Fire action is held (Auto) or on each press
    /// (Single), gated by <see cref="WeaponData.FireRate"/>.
    /// </summary>
    public sealed class Weapon : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private WeaponData data;
        [Tooltip("The ShipControls.inputactions asset containing the ship action map.")]
        [SerializeField] private InputActionAsset controls;
        [Tooltip("Where projectiles spawn and which way they aim (usually the ship's nose).")]
        [SerializeField] private Transform firePoint;

        [Header("Setup")]
        [SerializeField] private string actionMapName = "Ship";

        private InputActionMap _shipMap;
        private InputAction _fire;
        private float _cooldownRemaining;
        private float _releasedFor = float.MaxValue;
        private Collider _ownerCollider;
        private PlayerInventory _inventory;

        private void Awake()
        {
            if (data == null)
                Debug.LogError($"{nameof(Weapon)} on '{name}' has no {nameof(WeaponData)} assigned.", this);
            if (firePoint == null)
                Debug.LogError($"{nameof(Weapon)} on '{name}' has no fire point assigned.", this);
            // The hull's physical colliders live on nested child mesh pieces (Model/Cube,
            // Model/Cube (1)), not on this GameObject or an ancestor, so this has to search
            // down rather than up.
            _ownerCollider = GetComponentInChildren<Collider>();
            _inventory = GetComponentInParent<PlayerInventory>();

            if (controls != null)
            {
                _shipMap = controls.FindActionMap(actionMapName, throwIfNotFound: true);
                _fire = _shipMap.FindAction("Fire", throwIfNotFound: true);
            }
            else
            {
                Debug.LogError($"{nameof(Weapon)} on '{name}' has no {nameof(InputActionAsset)} assigned.", this);
            }
        }

        private void OnEnable() => _fire?.Enable();
        private void OnDisable() => _fire?.Disable();

        private void Update()
        {
            if (data == null || firePoint == null || _fire == null) return;

            _cooldownRemaining -= Time.deltaTime;
            bool isPressed = _fire.IsPressed();

            if (data.FireMode == WeaponFireMode.Auto)
            {
                if (isPressed && _cooldownRemaining <= 0f) Fire();
                return;
            }

            // Single: WasPressedThisFrame() has no protection against button/trigger contact
            // bounce, so one physical click can register as several rapid press edges. Instead,
            // a press only counts as NEW once the button has read released for at least
            // SingleFireDebounce seconds, filtering out that bounce.
            if (!isPressed)
            {
                _releasedFor += Time.deltaTime;
                return;
            }

            if (_releasedFor >= data.SingleFireDebounce && _cooldownRemaining <= 0f) Fire();
            _releasedFor = 0f;
        }

        private void Fire()
        {
            int effectiveDamage = _inventory != null
                ? _inventory.GetModifiedInt(ChipStatTarget.Damage, data.Damage)
                : data.Damage;
            float effectiveSplashRadius = _inventory != null
                ? _inventory.GetModifiedFloat(ChipStatTarget.SplashRadius, 0f)
                : 0f;
            float effectiveCorrosionDuration = _inventory != null
                ? _inventory.GetModifiedFloat(ChipStatTarget.CorrosionDuration, 0f)
                : 0f;
            int effectiveCorrosionDps = _inventory != null
                ? _inventory.GetModifiedInt(ChipStatTarget.CorrosionDps, 0)
                : 0;

            ProjectileLauncher.Fire(data.ProjectilePrefab, effectiveDamage, data.ProjectileSpeed, data.ProjectileLifetime, firePoint, _ownerCollider, effectiveSplashRadius, data.SplashVfxPrefab, data.HitMarkerPrefab, effectiveCorrosionDuration, effectiveCorrosionDps);

            float effectiveFireRate = _inventory != null
                ? _inventory.GetModifiedFloat(ChipStatTarget.FireRate, data.FireRate)
                : data.FireRate;
            _cooldownRemaining = 1f / Mathf.Max(effectiveFireRate, 0.0001f);
        }
    }
}
