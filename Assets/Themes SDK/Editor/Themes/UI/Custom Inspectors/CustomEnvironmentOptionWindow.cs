using System;
using System.Threading.Tasks;
using OpenUp.Environment;
using OpenUp.Networking.AWS;
using OpenUp.Networking.Editor;
using OpenUp.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace OpenUp.Editor.EnvironmentsSdk
{
    [CustomEditor(typeof(EnvironmentOption))]
    public class CustomEnvironmentOptionWindow : UnityEditor.Editor
    {
        private SerializedProperty rootObjProp;
        private SerializedProperty rootObjARProp;
        private SerializedProperty skyboxProp;
        private SerializedProperty sceneProp;
        private SerializedProperty modeProp;
        private PerformanceAnalysis analysis;
        private UnityWebRequest thumbReq;

        private new EnvironmentOption target => base.target as EnvironmentOption;

        private void CreateExport()
        {
            ExportWindow window = EditorWindow.GetWindow<ExportWindow>();
            
            window.SetAsset(target);
            window.Show();
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Export"))
            {
                CreateExport();
            }

            serializedObject.Update ();
            
            rootObjProp = serializedObject.FindProperty(nameof(EnvironmentOption.rootObject));
            rootObjARProp = serializedObject.FindProperty(nameof(EnvironmentOption.rootObjectAR));
            skyboxProp = serializedObject.FindProperty(nameof(EnvironmentOption.skybox));
            sceneProp = serializedObject.FindProperty(nameof(EnvironmentOption.scene));
            modeProp = serializedObject.FindProperty(nameof(EnvironmentOption.mode));
            
            EditorGUILayout.Space();
            
            // Export system decouples the environment option from the meta details and only has the scene and data.
            // RenderDetails();
            
            RenderStats();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Root", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(modeProp);
            EnvironmentOption.Mode mode = (EnvironmentOption.Mode)modeProp.intValue;

            if (mode == EnvironmentOption.Mode.RootObject)
            {
                EditorGUILayout.PropertyField(rootObjProp);
                EditorGUILayout.PropertyField(rootObjARProp);
                EditorGUILayout.PropertyField(skyboxProp);
            }
            else if (mode == EnvironmentOption.Mode.Scene)
            {
                EditorGUILayout.PropertyField(sceneProp);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            RenderObjectList();
            
            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            DoAnalysis();
        }

        private void RenderObjectList()
        {
            SerializedProperty prop = serializedObject.FindProperty(nameof(EnvironmentOption.themedObjects));
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
            if (analysis == null)
                return;
            
            PerformanceUI.DrawWarnings(analysis);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Recalculate")) 
                DoAnalysis();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField("Vertices", analysis.TotalVertices.ToString());
            EditorGUILayout.LabelField("Total Objects", analysis.Objects.Count.ToString());
            EditorGUILayout.LabelField("Interactable Objects", analysis.InteractableObjects.ToString());
            EditorGUILayout.LabelField("Colliders", analysis.ColliderCount.ToString());
            EditorGUILayout.LabelField("Mesh Collision Faces", analysis.MeshColliderFaces.ToString());
            
            EditorGUILayout.EndVertical();
        }

        private void DoAnalysis()
        {
            EnvironmentOption.Mode mode = ((EnvironmentOption)serializedObject.targetObject).mode;
            
            if (mode is EnvironmentOption.Mode.RootObject && target.rootObject.isSet)
            {
                analysis = PerformanceAnalysis.Analyse(target.rootObject.asset);
            }
            else if (mode is EnvironmentOption.Mode.Scene && target.scene)
            {
                Scene scene = SceneManager.GetSceneByPath(AssetDatabase.GetAssetPath(target.scene));
                if (scene.isLoaded)
                    analysis = PerformanceAnalysis.Analyse(scene.GetRootGameObjects());
            }
        }
    }
}