using OpenUp.Environment.Features;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Editor.ThemesSDK
{
    [CustomPropertyDrawer(typeof(ThemedObject))]
    public class ThemedObjectDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 64;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);
            float labelwidth = EditorGUIUtility.labelWidth;
            const float labelWidth = 144;
            EditorGUIUtility.labelWidth = labelWidth;
            
            SerializedProperty goProp = property.FindPropertyRelative(nameof(ThemedObject.GameObject));
            SerializedProperty nameProp = property.FindPropertyRelative(nameof(ThemedObject.DisplayName));
            SerializedProperty thumbProp = property.FindPropertyRelative(nameof(ThemedObject.Thumbnail));
            
            // Calculate rects
            var goRect = new Rect(position.x, position.y, position.width, 18);
            var nameRect = new Rect(position.x, position.y+20, position.width, 18);
            var thumbRect = new Rect(position.x, position.y+40, position.width, 20);
  
            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            EditorGUI.PropertyField(goRect, goProp, new GUIContent("GameObject"));
            EditorGUI.PropertyField(nameRect, nameProp, new GUIContent("Display Name"));
            EditorGUI.PropertyField(thumbRect, thumbProp, new GUIContent("Thumbnail"));

            EditorGUIUtility.labelWidth = labelwidth;            
            EditorGUI.EndProperty();

            var pathProp = property.FindPropertyRelative(nameof(ThemedObject.RelativePath));
            
            pathProp.stringValue = AssetDatabase.GetAssetPath(goProp.objectReferenceValue);
            pathProp.serializedObject.ApplyModifiedProperties();
        }

        private static SerializedProperty GetParent(SerializedProperty property)
            => property.serializedObject.FindProperty(
                property.propertyPath.Substring(0, property.propertyPath.LastIndexOf('.')));
    }
}