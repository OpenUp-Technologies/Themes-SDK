using System;
using System.IO;
using System.Numerics;
using OpenUp.Editor.EnvironmentsSdk;
using OpenUp.Environment.Features;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenUp.Editor.ThemesSDK
{
    [CustomPropertyDrawer(typeof(ThemedObject))]
    public class ThemedObjectDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 90;
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
            var buttonRect = new Rect(position.x+labelWidth, position.y+60, position.width - labelWidth, 20);
  
            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            EditorGUI.PropertyField(goRect, goProp, new GUIContent("GameObject"));
            EditorGUI.PropertyField(nameRect, nameProp, new GUIContent("Display Name"));
            EditorGUI.PropertyField(thumbRect, thumbProp, new GUIContent("Thumbnail"));
            
            if (GUI.Button(buttonRect, "Generate thumbnail"))
                GenerateNewThumbnail(property);

            EditorGUIUtility.labelWidth = labelwidth;            
            EditorGUI.EndProperty();

            var pathProp = property.FindPropertyRelative(nameof(ThemedObject.RelativePath));
            
            pathProp.stringValue = AssetDatabase.GetAssetPath(goProp.objectReferenceValue);
            pathProp.serializedObject.ApplyModifiedProperties();
        }
        
        private void GenerateNewThumbnail(SerializedProperty property)
        {
            SerializedProperty goProp = property.FindPropertyRelative(nameof(ThemedObject.GameObject));
            SerializedProperty thumbProp = property.FindPropertyRelative(nameof(ThemedObject.Thumbnail));
            
            string path = ThumbnailGenerator.GenerateForPrefab(goProp.objectReferenceValue as GameObject);
            string relative = Path.GetRelativePath(Application.dataPath, path);
            relative = $"Assets/{relative}";
            
            AssetDatabase.ImportAsset(relative, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(relative);
            
            Debug.Log($"Generated thumbnail at {relative}", tex);
            
            thumbProp.objectReferenceValue = tex;
        }

        private static SerializedProperty GetParent(SerializedProperty property)
            => property.serializedObject.FindProperty(
                property.propertyPath.Substring(0, property.propertyPath.LastIndexOf('.')));
    }
}