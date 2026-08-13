using System;
using UnityEngine;

namespace Manatite.Data
{
    [Serializable]
    public struct LootEntry
    {
        public ChipData item;
        [Range(0f, 1f)] public float dropChance;
    }

    /// <summary>
    /// Chips a cleared planet can drop into the player's inventory. Each entry rolls its
    /// own <see cref="LootEntry.dropChance"/> independently, so a single clear can drop
    /// zero, one, or several chips.
    /// </summary>
    [CreateAssetMenu(fileName = "LootTable_", menuName = "Manatite/Loot Table Data", order = 6)]
    public sealed class LootTableData : ScriptableObject
    {
        [SerializeField] private LootEntry[] entries = Array.Empty<LootEntry>();

        [Header("Shop")]
        [Tooltip("Chance, rolled independently of the chip entries above, that clearing this planet also spawns the chip-forging shop marker above it.")]
        [Range(0f, 1f)] [SerializeField] private float shopChance;

        public LootEntry[] Entries => entries;
        public float ShopChance => shopChance;
    }
}
