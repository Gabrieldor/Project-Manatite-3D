using UnityEngine;

namespace Manatite.Data
{
    /// <summary>
    /// Tunables for a chip/modifier item. A <see cref="Manatite.Core.ChipInstance"/> rolls
    /// one random value per <see cref="ChipModifier"/> (in [MinValue, MaxValue]) per copy,
    /// at creation, rather than every chip sharing this asset's value directly.
    /// </summary>
    [CreateAssetMenu(fileName = "Chip_", menuName = "Manatite/Chip Data", order = 5)]
    public sealed class ChipData : ScriptableObject
    {
        [SerializeField] private string displayName = "Chip";
        [SerializeField] private Sprite icon;
        [Tooltip("Every stat effect this chip applies while equipped. One chip can carry several.")]
        [SerializeField] private ChipModifier[] modifiers = { new ChipModifier() };
        [Tooltip("What this chip's modifier does, shown in the inventory tooltip's bottom 2/3.")]
        [TextArea]
        [SerializeField] private string description = "";

        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public ChipModifier[] Modifiers => modifiers;
        public string Description => description;

        /// <summary>True for an in-memory-only chip built by <see cref="CreateMerged"/> — has
        /// no backing .asset, so the save system must serialize its full definition inline
        /// instead of referencing it by name via <see cref="Manatite.Data.ChipDatabaseData"/>.</summary>
        public bool IsMerged { get; private set; }

        /// <summary>For a merged chip, the asset name of the real (non-merged) chip whose
        /// <see cref="Icon"/> this one inherited — <see cref="Manatite.Core.ChipMerger"/> always
        /// reuses a parent's icon, so this traces back through any depth of re-merging to a real
        /// <see cref="Manatite.Data.ChipDatabaseData"/> entry. Lets the save system reconstruct
        /// the icon on load instead of dropping it, since a raw <see cref="Sprite"/> reference
        /// can't portably round-trip through JSON. Unused (null) on a real, non-merged chip.</summary>
        public string IconSourceId { get; private set; }

        /// <summary>Builds an in-memory-only chip (never saved as an .asset) for
        /// <see cref="Manatite.Core.ChipMerger"/>'s output — merging shouldn't litter the
        /// project with one new asset per fuse.</summary>
        internal static ChipData CreateMerged(string displayName, Sprite icon, ChipModifier[] modifiers, string description, string iconSourceId)
        {
            ChipData chip = CreateInstance<ChipData>();
            chip.displayName = displayName;
            chip.icon = icon;
            chip.modifiers = modifiers;
            chip.description = description;
            chip.IsMerged = true;
            chip.IconSourceId = iconSourceId;
            return chip;
        }

        private void OnValidate()
        {
            for (int i = 0; i < modifiers.Length; i++)
                modifiers[i].ClampToValueKindRange();
        }
    }
}
