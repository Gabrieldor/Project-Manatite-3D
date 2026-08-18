using System;
using System.Collections.Generic;
using Manatite.Core;
using Manatite.Core.SaveSystem;
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

        [Tooltip("Chips placed directly into inventory slots 0.. at scene start (no pickup yet). " +
            "Skipped when resuming a save — RunSaveController.RestoreState replaces this instead.")]
        [SerializeField] private ChipData[] startingInventoryItems = Array.Empty<ChipData>();
        [Tooltip("Every real chip asset, used to resolve a saved chip back to its .asset on load.")]
        [SerializeField] private ChipDatabaseData chipDatabase;

        // Allocated inline (not in Awake) so any other object's OnEnable/Awake can safely read
        // these arrays regardless of cross-object script execution order.
        private readonly ChipInstance[] _equipSlots = new ChipInstance[EquipSlotCount];
        private readonly ChipInstance[] _inventorySlots = new ChipInstance[InventorySlotCount];

        /// <summary>Staging slots for the chip-forging shop's Merge panel. Kept separate from
        /// <see cref="_equipSlots"/>/<see cref="_inventorySlots"/> rather than a third
        /// <see cref="SlotsFor"/> category, since only the shop UI ever touches these.</summary>
        private readonly ChipInstance[] _mergeSlots = new ChipInstance[MergeSlotCount];

        /// <summary>The inventory slot each occupied merge slot's chip was last pulled from, so
        /// <see cref="ReturnMergeSlotsToInventory"/>/<see cref="PlaceMergeResultInInventory"/> can
        /// hand it back to the same spot instead of wherever the first empty slot happens to be.
        /// -1 for an empty merge slot.</summary>
        private readonly int[] _mergeSourceIndex = { -1, -1 };

        public event Action OnInventoryChanged;

        /// <summary>Fired whenever <see cref="TryAddItem"/> successfully places a chip, e.g.
        /// for a loot notification toast. Not fired for starting inventory items.</summary>
        public event Action<ChipInstance> OnItemAdded;

        private void Awake()
        {
            // A pending load replaces this wholesale via RestoreState (called from
            // RunSaveController.Start()) — don't bother rolling starting items just to discard them.
            if (PendingLoad.Data == null)
            {
                int count = Mathf.Min(startingInventoryItems.Length, _inventorySlots.Length);
                for (int i = 0; i < count; i++)
                {
                    if (startingInventoryItems[i] != null)
                        _inventorySlots[i] = new ChipInstance(startingInventoryItems[i]);
                }
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

        /// <summary>Clears a slot outright (no swap partner) — used to consume a chip like the
        /// repair chip, which disappears on use rather than moving elsewhere.</summary>
        public void RemoveSlot(bool equip, int index)
        {
            SlotsFor(equip)[index] = null;
            OnInventoryChanged?.Invoke();
        }

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
        /// already there, and removing one (by targeting an empty inventory slot). Refuses the
        /// swap (returns false, no state change) if the inventory chip moving into the merge
        /// slot is consumable (e.g. Repair) — a one-shot-use chip has nothing meaningful to
        /// fuse into another chip's stats.</summary>
        public bool SwapMergeWithInventory(int mergeIndex, int inventoryIndex)
        {
            ChipInstance incoming = _inventorySlots[inventoryIndex];
            if (incoming != null && incoming.Data.IsConsumable) return false;

            (_mergeSlots[mergeIndex], _inventorySlots[inventoryIndex]) = (_inventorySlots[inventoryIndex], _mergeSlots[mergeIndex]);
            _mergeSourceIndex[mergeIndex] = _mergeSlots[mergeIndex] != null ? inventoryIndex : -1;
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>Finds the first empty inventory slot, or -1 if none.</summary>
        private int FindFirstEmptyInventorySlot()
        {
            for (int i = 0; i < _inventorySlots.Length; i++)
                if (_inventorySlots[i] == null) return i;
            return -1;
        }

        /// <summary>Moves merge slot 0's (post-<see cref="TryMerge"/>) result into the inventory
        /// slot its first input came from if that's still empty, else the first empty slot —
        /// a merged chip shouldn't just sit in the merge row until the player happens to move it.
        /// Returns the inventory index it landed in, or -1 if the inventory was full (the chip
        /// stays in the merge slot rather than being lost).</summary>
        public int PlaceMergeResultInInventory()
        {
            if (_mergeSlots[0] == null) return -1;

            int target = _mergeSourceIndex[0] >= 0 && _inventorySlots[_mergeSourceIndex[0]] == null
                ? _mergeSourceIndex[0]
                : FindFirstEmptyInventorySlot();
            if (target < 0) return -1;

            SwapMergeWithInventory(0, target);
            return target;
        }

        /// <summary>Hands back whatever's sitting in either merge slot to the inventory slot it
        /// was pulled from (or the first empty slot, as a fallback) — called when the shop
        /// closes without merging, so browsing chips into the merge row never destroys or
        /// strands them there.</summary>
        public void ReturnMergeSlotsToInventory()
        {
            for (int i = 0; i < MergeSlotCount; i++)
            {
                if (_mergeSlots[i] == null) continue;

                int target = _mergeSourceIndex[i] >= 0 && _inventorySlots[_mergeSourceIndex[i]] == null
                    ? _mergeSourceIndex[i]
                    : FindFirstEmptyInventorySlot();
                if (target < 0) continue;

                SwapMergeWithInventory(i, target);
            }
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

        /// <summary>Snapshots equip + inventory slots for <see cref="RunSaveController.Save"/>.
        /// Merge slots are intentionally excluded — they're shop-UI staging, not persistent state.</summary>
        public InventorySaveData CaptureState()
        {
            var data = new InventorySaveData();
            CaptureSlots(_equipSlots, data.equipSlots);
            CaptureSlots(_inventorySlots, data.inventorySlots);
            return data;
        }

        /// <summary>Replaces every equip/inventory slot with saved state, resolving real chips
        /// via <see cref="chipDatabase"/> and rebuilding merged chips from their inline
        /// definition. Missing/short lists (e.g. an old save predating this feature) just leave
        /// the corresponding slots empty.</summary>
        public void RestoreState(InventorySaveData data)
        {
            if (data == null) return;

            RestoreSlots(data.equipSlots, _equipSlots);
            RestoreSlots(data.inventorySlots, _inventorySlots);
            OnInventoryChanged?.Invoke();
        }

        private static void CaptureSlots(ChipInstance[] slots, List<ChipSaveData> into)
        {
            foreach (ChipInstance chip in slots)
                into.Add(CaptureChip(chip));
        }

        private static ChipSaveData CaptureChip(ChipInstance chip)
        {
            if (chip == null) return new ChipSaveData();

            ChipModifier[] modifiers = chip.Data.Modifiers;
            var modifierData = new ChipModifierSaveData[modifiers.Length];
            for (int i = 0; i < modifiers.Length; i++)
                modifierData[i] = new ChipModifierSaveData { statTarget = modifiers[i].StatTarget, valueKind = modifiers[i].ValueKind };

            return new ChipSaveData
            {
                isEmpty = false,
                isMerged = chip.Data.IsMerged,
                chipId = chip.Data.IsMerged ? null : chip.Data.name,
                displayName = chip.Data.IsMerged ? chip.Data.DisplayName : null,
                description = chip.Data.IsMerged ? chip.Data.Description : null,
                iconSourceId = chip.Data.IsMerged ? chip.Data.IconSourceId : null,
                modifiers = chip.Data.IsMerged ? modifierData : Array.Empty<ChipModifierSaveData>(),
                rolledValues = (float[])chip.RolledValues.Clone()
            };
        }

        private void RestoreSlots(List<ChipSaveData> saved, ChipInstance[] into)
        {
            for (int i = 0; i < into.Length; i++)
                into[i] = saved != null && i < saved.Count ? RestoreChip(saved[i]) : null;
        }

        private ChipInstance RestoreChip(ChipSaveData saved)
        {
            if (saved == null || saved.isEmpty) return null;

            if (!saved.isMerged)
            {
                ChipData data = chipDatabase != null ? chipDatabase.FindById(saved.chipId) : null;
                return data != null ? new ChipInstance(data, saved.rolledValues) : null;
            }

            var modifiers = new ChipModifier[saved.modifiers.Length];
            for (int i = 0; i < modifiers.Length; i++)
                modifiers[i] = new ChipModifier(saved.modifiers[i].statTarget, saved.modifiers[i].valueKind, 0f, 0f);

            Sprite icon = chipDatabase != null ? chipDatabase.FindById(saved.iconSourceId)?.Icon : null;
            ChipData merged = ChipData.CreateMerged(saved.displayName, icon, modifiers, saved.description, saved.iconSourceId);
            return new ChipInstance(merged, saved.rolledValues);
        }
    }
}
