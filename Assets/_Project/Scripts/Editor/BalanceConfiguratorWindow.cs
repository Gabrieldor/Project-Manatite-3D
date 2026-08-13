using System;
using System.Linq;
using System.Reflection;
using Manatite.Data;
using UnityEditor;
using UnityEngine;

namespace Manatite.Editor
{
    /// <summary>
    /// One-stop tuning window: lists every numeric field on the project's Data
    /// ScriptableObjects and exposes it as a 0.1x-10x slider around a "standard" baseline
    /// (the value the field held the first time this window touched it). See
    /// Docs/specs/balance-configurator.md.
    /// </summary>
    public sealed class BalanceConfiguratorWindow : EditorWindow
    {
        private const string BaselineAssetPath = "Assets/_Project/Data/Balance/BalanceBaseline.asset";
        private const string DataFolder = "Assets/_Project/Data";
        private const float MinMultiplier = 0.1f;
        private const float MaxMultiplier = 10f;

        private static readonly Type[] GeneralDataTypes =
        {
            typeof(ShipMovementData),
            typeof(WeaponData),
            typeof(HealthData),
            typeof(TargetLockData),
        };

        private static readonly Type[] EnemyDataTypes =
        {
            typeof(EnemyData),
            typeof(EnemySpawnData),
        };

        private static readonly Type[] PlanetDataTypes =
        {
            typeof(PlanetSpawnData),
            typeof(PlanetData),
        };

        private static readonly Type[] ChipDataTypes =
        {
            typeof(ChipData),
        };

        private static readonly string[] TabLabels = { "General", "Enemies", "Planets", "Chips" };

        private BalanceBaseline baseline;
        private Vector2 scrollPosition;
        private int selectedTab;

        [MenuItem("Window/Manatite/Balance Configurator")]
        private static void Open()
        {
            var window = GetWindow<BalanceConfiguratorWindow>("Balance Configurator");
            window.minSize = new Vector2(460f, 320f);
        }

        private void OnEnable()
        {
            baseline = LoadOrCreateBaseline();
        }

        private static BalanceBaseline LoadOrCreateBaseline()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BalanceBaseline>(BaselineAssetPath);
            if (existing != null)
            {
                return existing;
            }

            var created = CreateInstance<BalanceBaseline>();
            AssetDatabase.CreateAsset(created, BaselineAssetPath);
            AssetDatabase.SaveAssets();
            return created;
        }

        private void OnGUI()
        {
            if (baseline == null)
            {
                EditorGUILayout.HelpBox("Baseline asset missing.", MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(
                "Sliders scale each value from 0.1x to 10x of its standard (baseline) value. " +
                "The baseline for a field is captured the first time it's shown here.",
                MessageType.Info);

            selectedTab = GUILayout.Toolbar(selectedTab, TabLabels);
            EditorGUILayout.Space(4f);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var dataTypes = selectedTab switch
            {
                1 => EnemyDataTypes,
                2 => PlanetDataTypes,
                3 => ChipDataTypes,
                _ => GeneralDataTypes,
            };
            foreach (var dataType in dataTypes)
            {
                DrawAssetsOfType(dataType);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAssetsOfType(Type dataType)
        {
            // ChipData exposes its tunables as a ChipModifier[] rather than top-level numeric
            // fields, so the generic reflection walk below (which only looks one level deep)
            // would find nothing to draw for it.
            if (dataType == typeof(ChipData))
            {
                DrawChipAssets();
                return;
            }

            // EnemySpawnData mixes top-level numerics with a non-numeric spawnMode enum and
            // a per-entry count buried in an EnemySpawnEntry[], neither of which the generic
            // reflection walk below can reach.
            if (dataType == typeof(EnemySpawnData))
            {
                DrawEnemySpawnAssets();
                return;
            }

            var numericFields = dataType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => (f.FieldType == typeof(float) || f.FieldType == typeof(int))
                            && (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null))
                .ToArray();

            if (numericFields.Length == 0)
            {
                return;
            }

            var guids = AssetDatabase.FindAssets($"t:{dataType.Name}", new[] { DataFolder });
            if (guids.Length == 0)
            {
                return;
            }

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                {
                    continue;
                }

                DrawAsset(asset, guid, numericFields);
            }
        }

        private void DrawAsset(ScriptableObject asset, string guid, FieldInfo[] numericFields)
        {
            EditorGUILayout.LabelField(asset.name, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            bool anyChanged = false;
            foreach (var field in numericFields)
            {
                var property = serializedObject.FindProperty(field.Name);
                if (property == null)
                {
                    continue;
                }

                anyChanged |= DrawField(guid, field.Name, ObjectNames.NicifyVariableName(field.Name), property);
            }

            if (anyChanged)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset all to standard", GUILayout.Width(160f)))
            {
                foreach (var field in numericFields)
                {
                    var property = serializedObject.FindProperty(field.Name);
                    if (property == null || !baseline.TryGetValue(guid, field.Name, out var standard))
                    {
                        continue;
                    }

                    ApplyValue(property, standard);
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }

            if (GUILayout.Button("Set all as standard", GUILayout.Width(160f)))
            {
                foreach (var field in numericFields)
                {
                    var property = serializedObject.FindProperty(field.Name);
                    if (property == null)
                    {
                        continue;
                    }

                    float current = property.propertyType == SerializedPropertyType.Integer
                        ? property.intValue
                        : property.floatValue;
                    baseline.SetValue(guid, field.Name, current);
                }

                SaveBaseline();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8f);
        }

        private bool DrawField(string guid, string fieldKey, string label, SerializedProperty property)
        {
            float currentValue = property.propertyType == SerializedPropertyType.Integer
                ? property.intValue
                : property.floatValue;

            if (!baseline.TryGetValue(guid, fieldKey, out var standard))
            {
                standard = currentValue;
                baseline.SetValue(guid, fieldKey, standard);
                SaveBaseline();
            }

            // A zero baseline has no meaningful multiplier (anything x0 stays 0) — fall back
            // to a plain editable field for these instead of a slider.
            if (Mathf.Approximately(standard, 0f))
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                float direct = EditorGUILayout.FloatField(label, currentValue);
                bool directChanged = EditorGUI.EndChangeCheck();

                bool setStdOnZero = GUILayout.Button("Set std", GUILayout.Width(55f));
                if (setStdOnZero)
                {
                    baseline.SetValue(guid, fieldKey, currentValue);
                    SaveBaseline();
                }

                EditorGUILayout.EndHorizontal();

                if (!directChanged)
                {
                    return false;
                }

                ApplyValue(property, direct);
                return true;
            }

            float multiplier = currentValue / standard;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(150f));

            EditorGUI.BeginChangeCheck();
            multiplier = GUILayout.HorizontalSlider(multiplier, MinMultiplier, MaxMultiplier, GUILayout.Width(150f));
            bool sliderChanged = EditorGUI.EndChangeCheck();

            float newValue = standard * multiplier;

            EditorGUI.BeginChangeCheck();
            newValue = EditorGUILayout.FloatField(newValue, GUILayout.Width(70f));
            bool fieldChanged = EditorGUI.EndChangeCheck();

            GUILayout.Label($"{multiplier:0.00}x", GUILayout.Width(45f));

            bool resetClicked = GUILayout.Button("Reset", GUILayout.Width(50f));
            if (resetClicked)
            {
                newValue = standard;
            }

            // Captures whatever the field currently holds (before this button's own click,
            // since Reset/slider/field edits this frame are handled below) as the new 1x
            // reference, without changing the value itself.
            bool setStdClicked = GUILayout.Button("Set std", GUILayout.Width(55f));
            if (setStdClicked)
            {
                baseline.SetValue(guid, fieldKey, currentValue);
                SaveBaseline();
            }

            EditorGUILayout.EndHorizontal();

            if (sliderChanged || fieldChanged || resetClicked)
            {
                ApplyValue(property, newValue);
                return true;
            }

            return false;
        }

        private void DrawChipAssets()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(ChipData)}", new[] { DataFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ChipData>(path);
                if (asset != null)
                {
                    DrawChipAsset(asset, guid);
                }
            }
        }

        private void DrawChipAsset(ChipData asset, string guid)
        {
            EditorGUILayout.LabelField(asset.name, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            var modifiersProperty = serializedObject.FindProperty("modifiers");
            bool anyChanged = false;
            var minKeys = new string[modifiersProperty.arraySize];
            var maxKeys = new string[modifiersProperty.arraySize];

            for (int i = 0; i < modifiersProperty.arraySize; i++)
            {
                var element = modifiersProperty.GetArrayElementAtIndex(i);
                var statTargetProperty = element.FindPropertyRelative("statTarget");
                var valueKindProperty = element.FindPropertyRelative("valueKind");
                var minProperty = element.FindPropertyRelative("minValue");
                var maxProperty = element.FindPropertyRelative("maxValue");

                string statTargetName = statTargetProperty.enumDisplayNames[statTargetProperty.enumValueIndex];
                string valueKindName = valueKindProperty.enumDisplayNames[valueKindProperty.enumValueIndex];
                EditorGUILayout.LabelField($"{statTargetName} ({valueKindName})", EditorStyles.miniBoldLabel);

                minKeys[i] = $"modifiers[{i}].minValue";
                maxKeys[i] = $"modifiers[{i}].maxValue";
                anyChanged |= DrawField(guid, minKeys[i], "Min", minProperty);
                anyChanged |= DrawField(guid, maxKeys[i], "Max", maxProperty);
            }

            if (anyChanged)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset all to standard", GUILayout.Width(160f)))
            {
                for (int i = 0; i < modifiersProperty.arraySize; i++)
                {
                    var element = modifiersProperty.GetArrayElementAtIndex(i);
                    ResetToStandard(guid, minKeys[i], element.FindPropertyRelative("minValue"));
                    ResetToStandard(guid, maxKeys[i], element.FindPropertyRelative("maxValue"));
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }

            if (GUILayout.Button("Set all as standard", GUILayout.Width(160f)))
            {
                for (int i = 0; i < modifiersProperty.arraySize; i++)
                {
                    var element = modifiersProperty.GetArrayElementAtIndex(i);
                    baseline.SetValue(guid, minKeys[i], element.FindPropertyRelative("minValue").floatValue);
                    baseline.SetValue(guid, maxKeys[i], element.FindPropertyRelative("maxValue").floatValue);
                }

                SaveBaseline();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8f);
        }

        private static readonly string[] EnemySpawnNumericFields =
        {
            "waveSize", "timeBetweenWaves", "minSpawnRadius", "maxSpawnRadius",
        };

        private void DrawEnemySpawnAssets()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(EnemySpawnData)}", new[] { DataFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<EnemySpawnData>(path);
                if (asset != null)
                {
                    DrawEnemySpawnAsset(asset, guid);
                }
            }
        }

        private void DrawEnemySpawnAsset(EnemySpawnData asset, string guid)
        {
            EditorGUILayout.LabelField(asset.name, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            var spawnModeProperty = serializedObject.FindProperty("spawnMode");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(spawnModeProperty, new GUIContent("Spawn Mode"));
            bool anyChanged = EditorGUI.EndChangeCheck();

            foreach (var fieldName in EnemySpawnNumericFields)
            {
                var property = serializedObject.FindProperty(fieldName);
                anyChanged |= DrawField(guid, fieldName, ObjectNames.NicifyVariableName(fieldName), property);
            }

            var entriesProperty = serializedObject.FindProperty("entries");
            EditorGUILayout.LabelField("Entries", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            var countKeys = new string[entriesProperty.arraySize];
            for (int i = 0; i < entriesProperty.arraySize; i++)
            {
                var element = entriesProperty.GetArrayElementAtIndex(i);
                var prefabProperty = element.FindPropertyRelative("enemyPrefab");
                var countProperty = element.FindPropertyRelative("count");

                string prefabName = prefabProperty.objectReferenceValue != null
                    ? prefabProperty.objectReferenceValue.name
                    : $"Entry {i}";
                countKeys[i] = $"entries[{i}].count";
                EditorGUILayout.LabelField(prefabName, EditorStyles.miniLabel);
                anyChanged |= DrawField(guid, countKeys[i], "Count", countProperty);
            }

            EditorGUI.indentLevel--;

            if (anyChanged)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset all to standard", GUILayout.Width(160f)))
            {
                foreach (var fieldName in EnemySpawnNumericFields)
                {
                    ResetToStandard(guid, fieldName, serializedObject.FindProperty(fieldName));
                }

                for (int i = 0; i < entriesProperty.arraySize; i++)
                {
                    var countProperty = entriesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("count");
                    ResetToStandard(guid, countKeys[i], countProperty);
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }

            if (GUILayout.Button("Set all as standard", GUILayout.Width(160f)))
            {
                foreach (var fieldName in EnemySpawnNumericFields)
                {
                    var property = serializedObject.FindProperty(fieldName);
                    float current = property.propertyType == SerializedPropertyType.Integer
                        ? property.intValue
                        : property.floatValue;
                    baseline.SetValue(guid, fieldName, current);
                }

                for (int i = 0; i < entriesProperty.arraySize; i++)
                {
                    var countProperty = entriesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("count");
                    baseline.SetValue(guid, countKeys[i], countProperty.intValue);
                }

                SaveBaseline();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8f);
        }

        private void ResetToStandard(string guid, string fieldKey, SerializedProperty property)
        {
            if (baseline.TryGetValue(guid, fieldKey, out var standard))
            {
                ApplyValue(property, standard);
            }
        }

        private void SaveBaseline()
        {
            EditorUtility.SetDirty(baseline);
            AssetDatabase.SaveAssets();
        }

        private static void ApplyValue(SerializedProperty property, float value)
        {
            if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = Mathf.Max(1, Mathf.RoundToInt(value));
            }
            else
            {
                property.floatValue = value;
            }
        }
    }
}
