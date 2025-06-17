using OpenUp.Environment.Features;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Editor.ThemesSDK
{
    [CustomPropertyDrawer(typeof(SpecialFeature.FeatureValue))]
    public class FeatureValueDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 44;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Calculate rects
            var typeRect = new Rect(position.x, position.y, 80, 18);
            var nameRect = new Rect(position.x + 90, position.y, position.width - 90, 18);
            var valueRect = new Rect(position.x + 90, position.y+20, position.width - 90, 20);
            
            SpecialFeature.FeatureValueType type = (SpecialFeature.FeatureValueType)property.FindPropertyRelative("type").enumValueIndex;

            string valueName = type switch
            {
                SpecialFeature.FeatureValueType.Bool => nameof(SpecialFeature.FeatureValue.boolValue),
                SpecialFeature.FeatureValueType.String => nameof(SpecialFeature.FeatureValue.stringValue),
                SpecialFeature.FeatureValueType.Vector3 => nameof(SpecialFeature.FeatureValue.vector3Value),
                SpecialFeature.FeatureValueType.Float => nameof(SpecialFeature.FeatureValue.floatValue),
                SpecialFeature.FeatureValueType.Int => nameof(SpecialFeature.FeatureValue.intValue),
                _ => nameof(SpecialFeature.FeatureValue.intValue)
            };

            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("type"), GUIContent.none);
            EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("name"), GUIContent.none);
            EditorGUI.PropertyField(valueRect, property.FindPropertyRelative(valueName), GUIContent.none);

            EditorGUI.EndProperty();
            
        }
    }
}