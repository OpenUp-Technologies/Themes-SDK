using System;
using OpenUp.Interpreter.Utils;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Editor.ThemesSDK
{
    [CustomEditor(typeof(ConvertToSolution))]
    public class ConvertToSolutionWindow : UnityEditor.Editor
    {
        private SerializedProperty sourceProp;
        private SerializedProperty noPhysicsProp;
        private SerializedProperty weightProp;
        private SerializedProperty centerOfMassProp;
        private SerializedProperty dragProp;
        private SerializedProperty isRootProp;
        private SerializedProperty childrenProp;
        
        private new ConvertToSolution target => base.target as ConvertToSolution;

        public void OnEnable()
        {
            sourceProp = serializedObject.FindProperty(nameof(ConvertToSolution.source));
            noPhysicsProp = serializedObject.FindProperty(nameof(ConvertToSolution.noPhysics));
            weightProp = serializedObject.FindProperty(nameof(ConvertToSolution.weight));
            centerOfMassProp = serializedObject.FindProperty(nameof(ConvertToSolution.centerOfMassObject));
            dragProp = serializedObject.FindProperty(nameof(ConvertToSolution.drag));
            isRootProp = serializedObject.FindProperty(nameof(ConvertToSolution.childOfRoot));
            childrenProp = serializedObject.FindProperty(nameof(ConvertToSolution.children));
        }

        public override void OnInspectorGUI()
        {
            RenderSourceFields();
            
            EditorGUILayout.PropertyField(noPhysicsProp);

            if (!noPhysicsProp.boolValue)
            {
                EditorGUILayout.PropertyField(weightProp);
                EditorGUILayout.PropertyField(centerOfMassProp);
                EditorGUILayout.PropertyField(dragProp);
            }
            
            EditorGUILayout.PropertyField(isRootProp);
            EditorGUILayout.PropertyField(childrenProp);
            
            serializedObject.ApplyModifiedProperties();
        }

        private void RenderSourceFields()
        {
            GameObject current = AssetDatabase.LoadAssetAtPath<GameObject>(sourceProp.stringValue);

            GameObject chosen = EditorGUILayout.ObjectField("Loaded object", current, typeof(GameObject), false) as GameObject;
            
            if (chosen != current)
            {
                sourceProp.stringValue = AssetDatabase.GetAssetPath(chosen);
            }
            
            string expected = target.GetExpectedPath();

            if (expected == null)
            {
                EditorGUILayout.LabelField(
                    "This object is not a prefab, a different object will be loaded in when the environment is loaded", 
                    EditorStyles.helpBox);
            }
            else if (expected != sourceProp.stringValue)
            {
                GUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField(
                    "A different object will be loaded in when the environment is loaded", 
                    EditorStyles.helpBox);
                
                if (GUILayout.Button("Fix", GUILayout.Width(50)))
                    target.SetPathToExpected();
                
                GUILayout.EndHorizontal();
            }
        }
    }
}