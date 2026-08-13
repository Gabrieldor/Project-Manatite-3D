using System;
using Manatite.Core;
using Manatite.Data;
using UnityEngine;

namespace Manatite.Player
{
    /// <summary>
    /// Backing data for the equip (4 slots) and inventory (36 slots) grids in
    /// <see cref="Manatite.UI.InventoryCursor"/>. Slot index within each grid matches the
    /// corresponding UI panel's child index. Also aggregates equipped chips into effective
    /// stat values for gameplay systems (e.g. <see cref="Manatite.Weapons.Weapon"/>).
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        private const int EquipSlotCount = 4;
        private const int InventorySlotCount = 36;
        private const int MergeSlotCount = 2;

        [Tooltip("Chips placed directly into inventory slots 0.. at scene start (no pickup yet).")]
        [SerializeField] private ChipData[] startingInventoryItems = Array.Empty<ChipData>();

        // Allocated inline (not in Awake) so any other object's OnEnable/Awake can safely read
        // these arrays regardless of cross-object script execution order.
        private readonly ChipInstance[] _equipSlots = new ChipInstance[EquipSlotCount];
        private readonly ChipInstance[] _inventorySlots = new ChipInstance[InventorySlotCount];

        /// <summary>Staging slots for the chip-forging shop's Merge panel. Kept separate from
        /// <see cref="_equipSlots"/>/<see cref="_inventorySlots"/> rather than a third
        /// <see cref="SlotsFor"/> category, since only the shop UI ever touches these.</summary>
        private readonly ChipInstance[] _mergeSlots = new ChipInstance[MergeSlotCount];

        public event Action OnInventoryChanged;

        /// <summary>Fired whenever <see cref="TryAddItem"/> successfully places a chip, e.g.
        /// for a loot notification toast. Not fired for starting inventory items.</summary>
        public event Action<ChipInstance> OnItemAdded;

        private void Awake()
        {
            int count = Mathf.Min(startingInventoryItems.Length, _inventorySlots.Length);
            for (int i = 0; i < count; i++)
            {
                if (startingInventoryItems[i] != null)
                    _inventorySlots[i] = new ChipInstance(startingInventoryItems[i]);
            }

            // Listeners may have already subscribed in their own OnEnable before this Awake ran.
            OnInventoryChanged?.Invoke();
        }

        /// <summary>Places a new instance of <paramref name="data"/> in the first empty
        /// inventory slot (equip slots are never auto-filled). Returns false and does
        /// nothing if the inventory is full.</summary>
        public bool TryAddItem(ChipData data)
        {
            for (int i = 0; i < _inventorySlots.Length; i++)
            {
                if (_inventorySlots[i] != null) continue;

                var instance = new ChipInstance(data);
                _inventorySlots[i] = instance;
                OnInventoryChanged?.Invoke();
                OnItemAdded?.Invoke(instance);
                return true;
            }
            return false;
        }

        public ChipInstance GetSlot(bool equip, int index) => SlotsFor(equip)[index];

        public bool IsSlotEmpty(bool equip, int index) => GetSlot(equip, index) == null;

        public void TrySwap(bool fromEquip, int fromIndex, bool toEquip, int toIndex)
        {
            ChipInstance[] from = SlotsFor(fromEquip);
            ChipInstance[] to = SlotsFor(toEquip);
            (from[fromIndex], to[toIndex]) = (to[toIndex], from[fromIndex]);
            OnInventoryChanged?.Invoke();
        }

        /// <summary>Applies every equipped chip modifier targeting <paramref name="target"/> to <paramref name="baseValue"/>: (base + sum of flat rolls) * (1 + sum of multiplier rolls).</summary>
        public float GetModifiedFloat(ChipStatTarget target, float baseValue)
        {
            float flatSum = 0f;
            float multiplierSum = 0f;

            foreach (ChipInstance chip in _equipSlots)
            {
                if (chip == null) continue;

                ChipModifier[] modifiers = chip.Data.Modifiers;
                for (int i = 0; i < modifiers.Length; i++)
                {
                    if (modifiers[i].StatTarget != target) continue;

                    if (modifiers[i].ValueKind == ChipValueKind.Flat) flatSum += chip.RolledValues[i];
                    else multiplierSum += chip.RolledValues[i];
                }
            }

            return (baseValue + flatSum) * (1f + multiplierSum);
        }

        /// <summary>Same as <see cref="GetModifiedFloat"/> but always rounds down, for int stats (damage, health, etc.).</summary>
        public int GetModifiedInt(ChipStatTarget target, int baseValue) =>
            Mathf.FloorToInt(GetModifiedFloat(target, baseValue));

        private ChipInstance[] SlotsFor(bool equip) => equip ? _equipSlots : _inventorySlots;

        public ChipInstance GetMergeSlot(int index) => _mergeSlots[index];

        public bool IsMergeSlotEmpty(int index) => _mergeSlots[index] == null;

        /// <summary>Swaps a merge slot's contents with an inventory slot's. Symmetric, so this
        /// single call covers placing a chip into an empty merge slot, replacing whatever was
        /// already there, and removing one (by targeting an empty inventory slot).</summary>
        public void SwapMergeWithInventory(int mergeIndex, int inventoryIndex)
        {
            (_mergeSlots[mergeIndex], _inventorySlots[inventoryIndex]) = (_inventorySlots[inventoryIndex], _mergeSlots[mergeIndex]);
            OnInventoryChanged?.Invoke();
        }

        /// <summary>Fuses both merge slots via <see cref="ChipMerger"/>: slot 0 becomes the
        /// result, slot 1 clears. No-ops (returns false) if either slot is empty.</summary>
        public bool TryMerge()
        {
            if (_mergeSlots[0] == null || _mergeSlots[1] == null) return false;

            _mergeSlots[0] = ChipMerger.Merge(_mergeSlots[0], _mergeSlots[1]);
            _mergeSlots[1] = null;
            OnInventoryChanged?.Invoke();
            return true;
        }
    }
}
