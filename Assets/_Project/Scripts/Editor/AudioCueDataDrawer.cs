using Manatite.Data;
using UnityEditor;
using UnityEngine;

namespace Manatite.Editor
{
    /// <summary>
    /// Adds a "Loop" checkbox to <see cref="AudioCueData"/> that reveals a min-max slider
    /// (clamped to clip 0's length) for picking loop-in/loop-out points, instead of guessing
    /// seconds by trial and error. See <see cref="Manatite.Audio.AudioManager.PlayLoopingSfx"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioCueData))]
    public sealed class AudioCueDataDrawer : PropertyDrawer
    {
        private const float FieldWidth = 50f;
        private const float FieldGap = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty clipsProp = property.FindPropertyRelative("clips");
            SerializedProperty loopProp = property.FindPropertyRelative("loop");

            float height = EditorGUI.GetPropertyHeight(clipsProp, true) + spacing;
            height += (lineHeight + spacing) * 3; // volumeRange, pitchRange, loop checkbox

            if (loopProp.boolValue)
                height += (lineHeight + spacing) * 2; // slider row + status label

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty clipsProp = property.FindPropertyRelative("clips");
            SerializedProperty volumeRangeProp = property.FindPropertyRelative("volumeRange");
            SerializedProperty pitchRangeProp = property.FindPropertyRelative("pitchRange");
            SerializedProperty loopProp = property.FindPropertyRelative("loop");
            SerializedProperty loopStartProp = property.FindPropertyRelative("loopStart");
            SerializedProperty loopEndProp = property.FindPropertyRelative("loopEnd");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            float clipsHeight = EditorGUI.GetPropertyHeight(clipsProp, true);
            var clipsRect = new Rect(position.x, position.y, position.width, clipsHeight);
            EditorGUI.PropertyField(clipsRect, clipsProp, new GUIContent("Clips"), true);

            var row = new Rect(position.x, clipsRect.yMax + spacing, position.width, lineHeight);

            EditorGUI.PropertyField(row, volumeRangeProp, new GUIContent("Volume Range"));
            row.y += lineHeight + spacing;

            EditorGUI.PropertyField(row, pitchRangeProp, new GUIContent("Pitch Range"));
            row.y += lineHeight + spacing;

            EditorGUI.PropertyField(row, loopProp, new GUIContent("Loop"));
            row.y += lineHeight + spacing;

            if (loopProp.boolValue)
            {
                var clip = clipsProp.arraySize > 0
                    ? clipsProp.GetArrayElementAtIndex(0).objectReferenceValue as AudioClip
                    : null;
                float clipLength = clip != null ? clip.length : 0f;
                float sliderMax = clipLength > 0f ? clipLength : 1f;
                float loopStart = loopStartProp.floatValue;
                float loopEnd = loopEndProp.floatValue;

                var minFieldRect = new Rect(row.x, row.y, FieldWidth, lineHeight);
                var maxFieldRect = new Rect(row.xMax - FieldWidth, row.y, FieldWidth, lineHeight);
                var sliderRect = new Rect(minFieldRect.xMax + FieldGap, row.y,
                    maxFieldRect.x - FieldGap - (minFieldRect.xMax + FieldGap), lineHeight);

                loopStart = EditorGUI.FloatField(minFieldRect, loopStart);
                EditorGUI.MinMaxSlider(sliderRect, ref loopStart, ref loopEnd, 0f, sliderMax);
                loopEnd = EditorGUI.FloatField(maxFieldRect, loopEnd);

                loopStartProp.floatValue = Mathf.Clamp(loopStart, 0f, sliderMax);
                loopEndProp.floatValue = Mathf.Clamp(loopEnd, 0f, sliderMax);
                row.y += lineHeight + spacing;

                string status = clip == null
                    ? "Assign a clip (element 0) to set loop points."
                    : loopEndProp.floatValue > loopStartProp.floatValue
                        ? $"Intro 0–{loopStartProp.floatValue:0.00}s, then loops {loopStartProp.floatValue:0.00}–{loopEndProp.floatValue:0.00}s."
                        : "Loop end must be greater than loop start — currently loops the whole clip.";
                EditorGUI.LabelField(row, status, EditorStyles.miniLabel);
            }

            EditorGUI.EndProperty();
        }
    }
}
