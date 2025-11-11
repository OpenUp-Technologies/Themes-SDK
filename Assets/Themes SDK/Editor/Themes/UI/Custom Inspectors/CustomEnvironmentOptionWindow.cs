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
        private class ImageUrlSource : IUrlSource
        {
            private readonly string environmentId;
            private readonly DeveloperProfile profile;
            
            public ImageUrlSource(string environmentId, DeveloperProfile profile)
            {
                this.environmentId = environmentId;
                this.profile = profile;
            }

            public async Task<UploadUrlResponse> GetUrl(string fileName)
            {
                var caller = await profile.GetSignedCaller();
                
                return await caller.PostAsync<UploadUrlResponse>(
                    $"environments/{environmentId}/upload_image_url", 
                    new { FileName = fileName });
            }
        }
        
        private enum Mode
        {
            [InspectorName("Root Object (Legacy)")]
            RootObject,
            [InspectorName("Scenes")]
            Scene
        }
        
        private SerializedProperty nameProp;
        private SerializedProperty descriptionProp;
        private SerializedProperty thumbnailProp;
        
        private SerializedProperty rootObjProp;
        private SerializedProperty rootObjARProp;
        private SerializedProperty skyboxProp;
        private SerializedProperty sceneProp;
        private PerformanceAnalysis analysis;
        private Mode mode;
        private UnityWebRequest thumbReq;

        private new EnvironmentOption target => base.target as EnvironmentOption;
        
        public override void OnInspectorGUI()
        {
            RenderStats();
            serializedObject.Update ();
            
            nameProp = serializedObject.FindProperty(nameof(EnvironmentOption.environmentName));
            descriptionProp = serializedObject.FindProperty(nameof(EnvironmentOption.description));
            thumbnailProp = serializedObject.FindProperty(nameof(EnvironmentOption.thumbnailImage));
            
            rootObjProp = serializedObject.FindProperty(nameof(EnvironmentOption.rootObject));
            rootObjARProp = serializedObject.FindProperty(nameof(EnvironmentOption.rootObjectAR));
            skyboxProp = serializedObject.FindProperty(nameof(EnvironmentOption.skybox));
            sceneProp = serializedObject.FindProperty(nameof(EnvironmentOption.scene));
            
            EditorGUILayout.Space();
            RenderDetails();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Root", EditorStyles.boldLabel);

            Mode oldMode = mode;
            mode = (Mode)EditorGUILayout.EnumPopup("Mode", mode);

            if (mode == Mode.RootObject)
            {
                EditorGUILayout.PropertyField(rootObjProp);
                EditorGUILayout.PropertyField(rootObjARProp);
                EditorGUILayout.PropertyField(skyboxProp);
            }
            else
            {
                EditorGUILayout.PropertyField(sceneProp);
            }
            
            if (mode != oldMode)
                DoAnalysis();
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            RenderObjectList();
            
            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            mode = target.scene ? Mode.Scene : Mode.RootObject;

            ReloadImage();
            DoAnalysis();
        }

        private void ReloadImage()
        {
            if (thumbReq is { isDone: true })
                thumbReq.Dispose();
            else if (thumbReq is { isDone: false })
                thumbReq.Abort();
            
            thumbReq = UnityWebRequestTexture.GetTexture(target.thumbnailImage);
            thumbReq.SendWebRequest();
        }

        private void RenderDetails()
        {
            EditorGUILayout.LabelField("Environment Details", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(nameProp);
            EditorGUILayout.PropertyField(descriptionProp);

            bool clicked;
            if (thumbReq is { isDone: true, result: UnityWebRequest.Result.Success })
            {
                DownloadHandlerTexture tex = thumbReq.downloadHandler as DownloadHandlerTexture;
                Rect rect = GUILayoutUtility.GetRect(0,0, GUILayout.ExpandWidth(true));
                GUILayout.Box(tex!.texture, GUILayout.Width(400), GUILayout.Height(tex.texture.height * 400 / tex.texture.width), GUILayout.ExpandWidth(true));
                
                rect.height = 24;
                rect.y += 10;
                rect.x += rect.width - 150;
                rect.width = 140;
                clicked = GUI.Button(rect, "Change Image");
            }
            else
            {
                clicked = GUILayout.Button("Set Image");
            }
            
            if (clicked)
            {
                ImageUploadWindow upload = EditorWindow.GetWindow<ImageUploadWindow>();
                
                upload.urlSource = new ImageUrlSource(target.databaseId, DeveloperProfile.Instance);
                upload.Setup(url =>
                {
                    thumbnailProp.stringValue = url;
                    thumbnailProp.serializedObject.ApplyModifiedProperties();
                    ReloadImage();
                });
            }

            if (GUILayout.Button("Apply Changes (affects all versions)"))
            {
                DeveloperProfile profile = DeveloperProfile.Instance;
                AsyncHelperEditor.DoAsync(ApplyChanges(profile));
            }
        }

        private void RenderObjectList()
        {
            SerializedProperty prop = serializedObject.FindProperty(nameof(EnvironmentOption.customObjects));
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
            if (mode is Mode.RootObject && target.rootObject.isSet)
            {
                analysis = PerformanceAnalysis.Analyse(target.rootObject.asset);
            }
            else if (mode is Mode.Scene && target.scene)
            {
                Scene scene = SceneManager.GetSceneByPath(AssetDatabase.GetAssetPath(target.scene));
                if (scene.isLoaded)
                    analysis = PerformanceAnalysis.Analyse(scene.GetRootGameObjects());
            }
        }

        private Func<Task> ApplyChanges(DeveloperProfile profile) => async () =>
        {
            var caller = await profile.GetSignedCaller();

            await caller.PutAsync(
                $"environments/{target.Id}/details",
                new { Name = target.environmentName, Description = target.description, Thumbnail = target.thumbnailImage });
            
            Debug.Log("Changes applied");
        };
    }
}