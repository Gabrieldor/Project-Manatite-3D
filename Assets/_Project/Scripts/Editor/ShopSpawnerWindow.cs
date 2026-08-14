using Manatite.Core;
using Manatite.Environment;
using Manatite.UI;
using UnityEditor;
using UnityEngine;

namespace Manatite.Editor
{
    /// <summary>
    /// Play-mode testing tool: spawns the chip-forging shop marker a fixed distance in front
    /// of the player, wired to the scene's <see cref="ShopPanel"/> exactly like a real
    /// planet-clear drop would — lets the panel/merge-slot flow be exercised without clearing
    /// a planet's encounter first.
    /// </summary>
    public sealed class ShopSpawnerWindow : EditorWindow
    {
        private const string ShopMarkerPrefabPath = "Assets/_Project/Prefabs/Environment/ShopMarker.prefab";

        private float _spawnDistance = 150f;

        [MenuItem("Window/Manatite/Shop Spawner")]
        private static void Open()
        {
            var window = GetWindow<ShopSpawnerWindow>("Shop Spawner");
            window.minSize = new Vector2(260f, 100f);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to spawn the shop marker.", MessageType.Info);
                return;
            }

            _spawnDistance = EditorGUILayout.FloatField("Distance in front", _spawnDistance);

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Spawn Shop", GUILayout.Height(28f)))
            {
                Spawn();
            }
        }

        private void Spawn()
        {
            GameObject player = GameObject.FindGameObjectWithTag(GameLayers.PlayerTag);
            if (player == null)
            {
                Debug.LogWarning("ShopSpawnerWindow: no GameObject tagged 'Player' found in the scene.");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopMarkerPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"ShopSpawnerWindow: prefab not found at {ShopMarkerPrefabPath}.");
                return;
            }

            var shopController = GameObject.Find("ShopController");
            ShopPanel shopPanel = shopController != null ? shopController.GetComponent<ShopPanel>() : null;
            if (shopPanel == null)
            {
                Debug.LogWarning("ShopSpawnerWindow: no 'ShopController' GameObject with a ShopPanel found in the scene.");
                return;
            }

            Transform playerTransform = player.transform;
            Vector3 spawnPos = playerTransform.position + playerTransform.forward * _spawnDistance;

            GameObject instance = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
            if (instance.TryGetComponent(out ShopMarker marker))
            {
                marker.Initialize(shopPanel);
            }

            // Parent under the nearest planet (world position unchanged) so this debug spawn is
            // findable by RunSaveController.Save()'s per-planet ShopMarker scan and survives a
            // save/reload exactly like a real planet-clear drop, instead of vanishing on load.
            PlanetState nearestPlanet = FindNearestPlanet(spawnPos);
            if (nearestPlanet != null)
                instance.transform.SetParent(nearestPlanet.transform, worldPositionStays: true);
        }

        private static PlanetState FindNearestPlanet(Vector3 position)
        {
            PlanetState[] planets = Object.FindObjectsByType<PlanetState>(FindObjectsSortMode.None);
            PlanetState nearest = null;
            float nearestSqrDistance = float.MaxValue;
            foreach (PlanetState planet in planets)
            {
                float sqrDistance = (planet.transform.position - position).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = planet;
                }
            }
            return nearest;
        }
    }
}
