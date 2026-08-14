using Manatite.Combat;
using Manatite.Data;
using UnityEngine;

namespace Manatite.Player
{
    /// <summary>
    /// Keeps the player ship's <see cref="Health"/> cap synced to equipped MaxHealth chips.
    /// Current HP is clamped (never auto-healed) when the cap changes, so equipping/unequipping
    /// a chip never tops up unrelated battle damage.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class PlayerHealthChipBridge : MonoBehaviour
    {
        private Health _health;
        private PlayerInventory _inventory;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _inventory = GetComponentInParent<PlayerInventory>();
            // Runs before Health.OnEnable, so its "_current = Max" picks up the chip-adjusted
            // cap immediately on spawn.
            ApplyMax();
        }

        private void OnEnable()
        {
            if (_inventory != null) _inventory.OnInventoryChanged += ApplyMax;
        }

        private void OnDisable()
        {
            if (_inventory != null) _inventory.OnInventoryChanged -= ApplyMax;
        }

        private void ApplyMax()
        {
            int newMax = _inventory != null
                ? _inventory.GetModifiedInt(ChipStatTarget.MaxHealth, _health.BaseMax)
                : _health.BaseMax;
            _health.SetMaxHealth(newMax);
            _health.SetCurrent(_health.Current);
        }
    }
}
