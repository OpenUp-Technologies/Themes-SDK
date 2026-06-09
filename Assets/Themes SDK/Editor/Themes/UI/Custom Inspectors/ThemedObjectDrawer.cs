using System;
using System.Collections.Generic;
using OpenUp.Editor.EnvironmentsSdk;
using OpenUp.Environment;
using OpenUp.Environment.Features;
using OpenUp.Networking.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace OpenUp.Editor.ThemesSDK
{
    [CustomPropertyDrawer(typeof(ThemedObject))]
    public class ThemedObjectDrawer : PropertyDrawer, IDisposable
    {
        private readonly Dictionary<string, UnityWebRequest> thumbs = new Dictionary<string, UnityWebRequest>();
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 124;
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
            var thumbRect = new Rect(position.x + labelWidth, position.y+40, position.width-labelWidth, 80);
            var thumbButton = new Rect(position.x , position.y+40, labelWidth - 12, 18);
  
            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            EditorGUI.PropertyField(goRect, goProp, new GUIContent("GameObject"));
            EditorGUI.PropertyField(nameRect, nameProp, new GUIContent("Display Name"));

            bool change = GUI.Button(thumbButton, "Change Image");
            
            var thumbReq = thumbs.GetValueOrDefault(thumbProp.stringValue);
            
            if (thumbReq is { isDone: true, result: UnityWebRequest.Result.Success })
            {
                DownloadHandlerTexture tex = thumbReq.downloadHandler as DownloadHandlerTexture;
                GUI.Box(thumbRect, tex!.texture);
            }
           
            EditorGUIUtility.labelWidth = labelwidth;            
            EditorGUI.EndProperty();

            if (change) 
                ChangeImage(thumbProp);
            
            ReloadImage(thumbProp.stringValue);
            
            var pathProp = property.FindPropertyRelative(nameof(ThemedObject.RelativePath));
            
            pathProp.stringValue = AssetDatabase.GetAssetPath(goProp.objectReferenceValue);
            pathProp.serializedObject.ApplyModifiedProperties();
        }

        private void ChangeImage(SerializedProperty thumbProp)
        {
            ImageUploadWindow upload = EditorWindow.GetWindow<ImageUploadWindow>();
            
            SerializedProperty parent = GetParent(thumbProp);
            EnvironmentOption option = parent.serializedObject.targetObject as EnvironmentOption;
            
            upload.urlSource = new ImageUrlSource(option!.databaseId, DeveloperProfile.Instance);
            upload.Setup(url =>
            {
                thumbProp.stringValue = url;
                thumbProp.serializedObject.ApplyModifiedProperties();
            }, ImageRatio.Square);
        }

        private void ReloadImage(string url)
        {
            if (string.IsNullOrEmpty(url)) 
                return;
            if (thumbs.ContainsKey(url)) 
                return;
            
            UnityWebRequest thumbReq = UnityWebRequestTexture.GetTexture(url);
            thumbReq.SendWebRequest();
            
            thumbs.Add(url, thumbReq);
        }
        
        public void Dispose()
        {
            foreach ((_, UnityWebRequest req) in thumbs)
            {
                if (req is { isDone: true})
                    req.Dispose();
                else if (req is { isDone: false})
                    req.Abort();
            }
            
            thumbs.Clear();
        }

        private static SerializedProperty GetParent(SerializedProperty property)
            => property.serializedObject.FindProperty(
                property.propertyPath.Substring(0, property.propertyPath.LastIndexOf('.')));
    }
}