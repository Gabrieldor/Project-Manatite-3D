using UnityEngine;

namespace Manatite.Data
{
    /// <summary>
    /// Every real (asset-backed) <see cref="ChipData"/> in the game, keyed by asset name.
    /// The runtime-safe equivalent of an editor-only <c>AssetDatabase</c> lookup — used by the
    /// save system to resolve a saved chip id back to its asset in a build, where
    /// <c>AssetDatabase</c> isn't available. Merged chips (<see cref="ChipData.CreateMerged"/>)
    /// have no asset and are never looked up here.
    /// </summary>
    [CreateAssetMenu(fileName = "ChipDatabase", menuName = "Manatite/Chip Database", order = 7)]
    public sealed class ChipDatabaseData : ScriptableObject
    {
        [SerializeField] private ChipData[] chips = System.Array.Empty<ChipData>();

        public ChipData FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            foreach (ChipData chip in chips)
                if (chip != null && chip.name == id)
                    return chip;
            return null;
        }
    }
}
