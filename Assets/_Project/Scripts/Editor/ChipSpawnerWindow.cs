using Manatite.Core;
using Manatite.Data;
using Manatite.Player;
using UnityEditor;
using UnityEngine;

namespace Manatite.Editor
{
    /// <summary>
    /// Play-mode testing tool: grants any real (non-merged) <see cref="ChipData"/> asset to the
    /// player's inventory on demand, without needing to clear a planet or roll loot for it.
    /// </summary>
    public sealed class ChipSpawnerWindow : EditorWindow
    {
        private const string ChipFolder = "Assets/_Project/Data/Weapons";

        private Vector2 _scroll;

        [MenuItem("Window/Manatite/Chip Spawner")]
        private static void Open()
        {
            var window = GetWindow<ChipSpawnerWindow>("Chip Spawner");
            window.minSize = new Vector2(260f, 200f);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to grant chips.", MessageType.Info);
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:ChipData", new[] { ChipFolder });
            if (guids.Length == 0)
            {
                EditorGUILayout.HelpBox($"No ChipData assets found under {ChipFolder}.", MessageType.Warning);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (string guid in guids)
            {
                var chip = AssetDatabase.LoadAssetAtPath<ChipData>(AssetDatabase.GUIDToAssetPath(guid));
                if (chip == null) continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(chip.DisplayName);
                if (GUILayout.Button("Add", GUILayout.Width(60f))) Grant(chip);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private static void Grant(ChipData chip)
        {
            GameObject player = GameObject.FindGameObjectWithTag(GameLayers.PlayerTag);
            if (player == null)
            {
                Debug.LogWarning("ChipSpawnerWindow: no GameObject tagged 'Player' found in the scene.");
                return;
            }

            PlayerInventory inventory = player.GetComponentInChildren<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogWarning("ChipSpawnerWindow: no PlayerInventory found on/under the 'Player' GameObject.");
                return;
            }

            if (!inventory.TryAddItem(chip))
                Debug.LogWarning($"ChipSpawnerWindow: inventory is full, couldn't add '{chip.DisplayName}'.");
        }
    }
}
