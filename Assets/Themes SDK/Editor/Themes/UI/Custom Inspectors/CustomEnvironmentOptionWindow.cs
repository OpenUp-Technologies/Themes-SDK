using System;
using OpenUp.Environment;
using UnityEditor;

namespace OpenUp.Editor.EnvironmentsSdk
{
    [CustomEditor(typeof(EnvironmentOption))]
    public class CustomEnvironmentOptionWindow : UnityEditor.Editor
    {
        private SerializedProperty nameProp;
        private SerializedProperty rootObjProp;
        private SerializedProperty rootObjARProp;
        private SerializedProperty skyboxProp;
        private PerformanceAnalysis analysis;

        private new EnvironmentOption target => base.target as EnvironmentOption;
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update ();
            
            nameProp = serializedObject.FindProperty(nameof(EnvironmentOption.environmentName));
            rootObjProp = serializedObject.FindProperty(nameof(EnvironmentOption.rootObject));
            rootObjARProp = serializedObject.FindProperty(nameof(EnvironmentOption.rootObjectAR));
            skyboxProp = serializedObject.FindProperty(nameof(EnvironmentOption.skybox));

            RenderStats();
            
            EditorGUILayout.PropertyField(nameProp);
            EditorGUILayout.PropertyField(rootObjProp);
            EditorGUILayout.PropertyField(rootObjARProp);
            EditorGUILayout.PropertyField(skyboxProp);
            
            RenderObjectList();
            
            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            analysis = PerformanceAnalysis.Analyse(target.rootObject.asset);
        }

        private void RenderObjectList()
        {
            SerializedProperty prop = serializedObject.FindProperty(nameof(EnvironmentOption.prefabs));
            EditorGUILayout.PropertyField(prop, true);
            
            SetPrefabPaths();
        }

        private void SetPrefabPaths()
        {
            SerializedProperty prefabs = serializedObject.FindProperty(nameof(EnvironmentOption.prefabs));
            SerializedProperty paths = serializedObject.FindProperty(nameof(EnvironmentOption.prefabPaths));
            
            paths.arraySize = prefabs.arraySize;
            int i = 0;
            foreach (SerializedProperty path in paths)
            {
                string prefabPath = AssetDatabase.GetAssetPath(prefabs.GetArrayElementAtIndex(i).objectReferenceValue);
                    
                if (path.stringValue != prefabPath) path.stringValue = prefabPath;
                
                i++;
            }
        }

        private void RenderStats()
        {
            PerformanceUI.DrawWarnings(analysis);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Vertices", analysis.TotalVertices.ToString());
            EditorGUILayout.LabelField("Total Objects", analysis.Objects.Count.ToString());
            EditorGUILayout.LabelField("Interactable Objects", analysis.InteractableObjects.ToString());
            EditorGUILayout.LabelField("Colliders", analysis.ColliderCount.ToString());
            EditorGUILayout.LabelField("Mesh Collision Faces", analysis.MeshColliderFaces.ToString());
            
            EditorGUILayout.EndVertical();
        }
    }
}