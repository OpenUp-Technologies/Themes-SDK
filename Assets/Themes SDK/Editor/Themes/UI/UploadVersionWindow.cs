using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenUp.Environment;
using OpenUp.Interpreter.Environment;
using OpenUp.Networking;
using OpenUp.Networking.Editor;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Editor.EnvironmentsSdk
{
    /// <summary>
    /// A GUI to upload a new version of an environment.
    /// </summary>
    public class UploadVersionWindow : EditorWindow
    {
        /// <summary>
        /// The environment you want to add a version to
        /// </summary>
        public RemoteOption environment;

        /// <summary>
        /// Set this if you want to edit an existing environment instead of creating a new one.
        /// </summary>
        public RemoteVersion existingVersion;

        private string versionCode;
        private string codeIssue;
        private PerformanceAnalysis analysis;
        
        // Only way to do this due to the upload being async...
        private bool isFinished;
        
        private EnvironmentOption localAsset;
        private readonly HashSet<Platform> chosenPlatforms = new HashSet<Platform>();

        public void SetAsset(EnvironmentOption option)
        {
            localAsset = option;
            analysis = PerformanceAnalysis.Analyse(localAsset.rootObject.asset);
        }

        private void OnGUI()
        {
            if (isFinished || environment == null)
            {
                Close();
                return;
            }

            titleContent = new GUIContent(environment.Name);
            
            EditorGUILayout.LabelField("Name", environment.Name);
            EditorGUILayout.LabelField("Id",   environment.Id);

            if (existingVersion == null)
            {
                versionCode = EditorGUILayout.DelayedTextField("Version", versionCode);
                ValidateVersionCode();
            }
            else
            {
                GUI.enabled = false;
                versionCode = EditorGUILayout.TextField("Version", existingVersion.BundleVersion);
                GUI.enabled = true;
            }

            localAsset = EditorGUILayout.ObjectField("Environment Asset", localAsset, typeof(EnvironmentOption), false) as EnvironmentOption;

            PerformanceUI.DrawWarnings(analysis);
            
            RenderPlatformSelection();
            RenderUploadButton();
            
            if (localAsset) PreviewAsset();
        }

        private void PreviewAsset()
        {
            GUI.enabled = false;
                
            EditorGUILayout.ObjectField("Root Object",    localAsset.rootObject.asset, typeof(GameObject), false);
            EditorGUILayout.ObjectField("Root Object AR",    localAsset.rootObjectAR.asset, typeof(GameObject), false);
            EditorGUILayout.ObjectField("Sky box Object", localAsset.skybox.asset,     typeof(Material),   false);
            EditorGUILayout.LabelField("Environment Objects", EditorStyles.boldLabel);
            
            EditorGUILayout.DelayedIntField("Size", localAsset.prefabs.Length);
           
            foreach (GameObject prefab in localAsset.prefabs)
            {
                EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
            }
            
            GUI.enabled = true;
        }

        private void RenderPlatformSelection()
        {
            string plats = chosenPlatforms.Aggregate("", (s, p) => String.IsNullOrEmpty(s) ? p.ToString() : $"{s}, {p}");

            if (GUILayout.Button(String.IsNullOrWhiteSpace(plats) ? "<Choose Build Targets>" : plats))
            {
                GenericMenu menu = new GenericMenu();
                
                foreach (Platform platform in Enum.GetValues(typeof(Platform)))
                {
                    void ChosePlat(object plat)
                    {
                        if (!chosenPlatforms.Contains(platform)) chosenPlatforms.Add(platform);
                        else chosenPlatforms.Remove(platform);
                    }

                    menu.AddItem(new GUIContent(platform.ToString()), chosenPlatforms.Contains(platform), ChosePlat, new object());
                }
                
                menu.ShowAsContext();
            }
        }
        
        private void ValidateVersionCode()
        {
            codeIssue = null;
            if (versionCode == null) return;

            if (Version.TryParse(versionCode, out Version version))
            {
                if (environment.Versions.Any(v => Version.Parse(v.BundleVersion) == version))
                {
                    codeIssue = "That version already exists";
                }
            }
            else
            {
                codeIssue = "This is not a valid version code";
            }
            
            if (codeIssue != null) EditorGUILayout.LabelField(codeIssue, EditorStyles.helpBox);
        }

        private void RenderUploadButton()
        {
            const string uploadText = "Upload new version";
            string content = null;

            if (existingVersion == null && versionCode == null)
            {
                content = "Must give the version a version code";
            }
            else if (existingVersion == null && codeIssue != null)
            {
                content = "Must have a valid version code";
            }
            else if (localAsset == null)
            {
                content = "Must choose a source asset to upload";
            }
            else if (chosenPlatforms.Count == 0)
            {
                content = "Choose 1 or more build targets";
            }
            else
            {
                content = uploadText;
            }

            GUI.enabled = content == uploadText;

            if (GUILayout.Button(content)) UploadVersion();

            GUI.enabled = true;
        }

        private void UploadVersion()
        {
            DeveloperProfile profile = DeveloperProfile.Instance;
            
            Platform[] platforms = chosenPlatforms.ToArray();

            Bundler              bundler = new Bundler(localAsset);
            Bundler.BundledEnv[] bundles = bundler.BuildBundles(platforms);

            Task.Run(() => DoAsyncUploadWork(profile, bundles));
        }

        private async Task DoAsyncUploadWork(DeveloperProfile developerProfile, Bundler.BundledEnv[] bundles)
        {
            try
            {
                EnvUploader uploader = new EnvUploader(developerProfile, environment);
                
                Dictionary<Platform, RemoteBundle> uploaded = await uploader.UploadAllBuildsAsync(bundles);


                RemoteVersion newVersion = existingVersion ?? new RemoteVersion
                                                              {
                                                                  BundleVersion = versionCode,
                                                                  AppVersion = "2023.0.0"
                                                              };

                await uploader.ConfirmUploadOf(newVersion, localAsset.prefabPaths, uploaded);

                isFinished = true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}