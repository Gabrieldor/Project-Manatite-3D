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
        private const string BaselineAssetPath = "Assets/_Project/Data/BalanceBaseline.asset";
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

        private static readonly string[] TabLabels = { "General", "Enemies", "Planets" };

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

                anyChanged |= DrawField(guid, field, property);
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

        private bool DrawField(string guid, FieldInfo field, SerializedProperty property)
        {
            float currentValue = property.propertyType == SerializedPropertyType.Integer
                ? property.intValue
                : property.floatValue;

            if (!baseline.TryGetValue(guid, field.Name, out var standard))
            {
                standard = currentValue;
                baseline.SetValue(guid, field.Name, standard);
                SaveBaseline();
            }

            var label = ObjectNames.NicifyVariableName(field.Name);

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
                    baseline.SetValue(guid, field.Name, currentValue);
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
                baseline.SetValue(guid, field.Name, currentValue);
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
