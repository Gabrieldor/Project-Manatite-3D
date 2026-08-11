using Manatite.Core;
using Manatite.Enemies;
using UnityEditor;
using UnityEngine;

namespace Manatite.Editor
{
    /// <summary>
    /// Play-mode testing tool: spawns one of the three enemy prefabs a fixed distance in
    /// front of the player, pooled and initialized exactly like <see cref="EnemySpawnZone"/>
    /// would spawn it in a real encounter.
    /// </summary>
    public sealed class EnemySpawnerWindow : EditorWindow
    {
        private static readonly string[] EnemyLabels = { "Basic", "Medium", "Large" };
        private static readonly string[] EnemyPrefabPaths =
        {
            "Assets/_Project/Prefabs/Enemies/BasicEnemy.prefab",
            "Assets/_Project/Prefabs/Enemies/MediumEnemy.prefab",
            "Assets/_Project/Prefabs/Enemies/LargeEnemy.prefab",
        };

        private int _selectedEnemy;
        private float _spawnDistance = 100f;

        [MenuItem("Window/Manatite/Enemy Spawner")]
        private static void Open()
        {
            var window = GetWindow<EnemySpawnerWindow>("Enemy Spawner");
            window.minSize = new Vector2(260f, 120f);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to spawn enemies.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Enemy type", EditorStyles.boldLabel);
            _selectedEnemy = GUILayout.Toolbar(_selectedEnemy, EnemyLabels);

            EditorGUILayout.Space(4f);
            _spawnDistance = EditorGUILayout.FloatField("Distance in front", _spawnDistance);

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Spawn", GUILayout.Height(28f)))
            {
                Spawn();
            }
        }

        private void Spawn()
        {
            GameObject player = GameObject.FindGameObjectWithTag(GameLayers.PlayerTag);
            if (player == null)
            {
                Debug.LogWarning("EnemySpawnerWindow: no GameObject tagged 'Player' found in the scene.");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPaths[_selectedEnemy]);
            if (prefab == null)
            {
                Debug.LogWarning($"EnemySpawnerWindow: prefab not found at {EnemyPrefabPaths[_selectedEnemy]}.");
                return;
            }

            Transform playerTransform = player.transform;
            Vector3 spawnPos = playerTransform.position + playerTransform.forward * _spawnDistance;
            Quaternion rotation = Quaternion.LookRotation(-playerTransform.forward, Vector3.up);

            GameObject instance = ObjectPooler.Get(prefab, spawnPos, rotation);

            Transform aimTarget = playerTransform.Find("CameraMount") ?? playerTransform;
            if (instance.TryGetComponent(out EnemyController controller))
            {
                controller.Initialize(aimTarget);
            }
        }
    }
}
