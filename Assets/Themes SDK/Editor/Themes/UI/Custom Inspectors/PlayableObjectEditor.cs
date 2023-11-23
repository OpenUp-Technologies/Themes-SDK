using System;
using OpenUp.Interpreter.Utils;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Editor.ThemesSDK
{
    [CustomEditor(typeof(PlayableObject))]
    public class PlayableObjectEditor : UnityEditor.Editor
    {
        private SerializedProperty sourceProp;
        private SerializedProperty noPhysicsProp;
        private SerializedProperty weightProp;
        private SerializedProperty centerOfMassProp;
        private SerializedProperty dragProp;
        private SerializedProperty isRootProp;
        private SerializedProperty childrenProp;
        
        private new PlayableObject target => base.target as PlayableObject;

        public void OnEnable()
        {
            sourceProp = serializedObject.FindProperty(nameof(PlayableObject.source));
            noPhysicsProp = serializedObject.FindProperty(nameof(PlayableObject.noPhysics));
            weightProp = serializedObject.FindProperty(nameof(PlayableObject.weight));
            centerOfMassProp = serializedObject.FindProperty(nameof(PlayableObject.centerOfMassObject));
            dragProp = serializedObject.FindProperty(nameof(PlayableObject.drag));
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

            PlayableObject outerConverter = target.transform.parent?.GetComponentInParent<PlayableObject>();
            
            if (outerConverter)
                CheckForDuplication(outerConverter);
        }

        private void CheckForDuplication(PlayableObject outerConverter)
        {
#if UNITY_2022_3
            GameObject sourcy = PrefabUtility.GetOriginalSourceRootWhereGameObjectIsAdded(target.gameObject); 
            GameObject outer = AssetDatabase.LoadAssetAtPath<GameObject>(outerConverter.source);

            if (sourcy == outer)
            {
                EditorGUILayout.HelpBox(
                    $"{target.gameObject.name} is already a child of prefab {outer.name}. During play {target.gameObject.name} will be instantiated twice.", 
                    MessageType.Warning);

                if (EditorGUILayout.LinkButton("Click her for more info"))
                    Help.BrowseURL("https://github.com/OpenUp-Technologies/Themes-SDK/blob/main/Documentation/Articles/ConvertToSolution.md#possible-object-duplication");
            }
#endif
        }
    }
}