using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manatite.Editor
{
    [Serializable]
    public struct BalanceBaselineEntry
    {
        public string assetGuid;
        public string fieldName;
        public float value;
    }

    /// <summary>
    /// Snapshot of "standard" (1x) values for tunable fields, keyed by (asset GUID, field
    /// name). Populated lazily by <see cref="BalanceConfiguratorWindow"/> the first time it
    /// sees a field — whatever value is in the asset at that moment becomes its 1x anchor.
    /// </summary>
    [CreateAssetMenu(fileName = "BalanceBaseline", menuName = "Manatite/Balance Baseline", order = 100)]
    public sealed class BalanceBaseline : ScriptableObject
    {
        [SerializeField] private List<BalanceBaselineEntry> entries = new List<BalanceBaselineEntry>();

        public bool TryGetValue(string assetGuid, string fieldName, out float value)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].assetGuid == assetGuid && entries[i].fieldName == fieldName)
                {
                    value = entries[i].value;
                    return true;
                }
            }

            value = 0f;
            return false;
        }

        public void SetValue(string assetGuid, string fieldName, float value)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].assetGuid == assetGuid && entries[i].fieldName == fieldName)
                {
                    var entry = entries[i];
                    entry.value = value;
                    entries[i] = entry;
                    return;
                }
            }

            entries.Add(new BalanceBaselineEntry { assetGuid = assetGuid, fieldName = fieldName, value = value });
        }
    }
}
