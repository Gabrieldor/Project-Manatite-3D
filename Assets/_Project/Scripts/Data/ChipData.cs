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

        private void OnValidate()
        {
            for (int i = 0; i < modifiers.Length; i++)
                modifiers[i].ClampToValueKindRange();
        }
    }
}
