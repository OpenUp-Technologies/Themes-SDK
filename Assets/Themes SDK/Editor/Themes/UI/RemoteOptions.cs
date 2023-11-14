using System;
using System.Collections.Generic;
using System.Linq;
using OpenUp.Environment;
using OpenUp.Interpreter.Environment;
using OpenUp.Networking.Editor;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public class RemoteOptions
    {
        private readonly EnvironmentsEditor  environmentsEditor;
        private bool fetchIsRunning => environmentsEditor.isFetchingRemotes;
        private List<RemoteOption> options => environmentsEditor.remoteOptions;
        private EnvironmentOption[] localOptions => environmentsEditor.localOptions;

        public RemoteOptions(EnvironmentsEditor environmentsEditor)
        {
            this.environmentsEditor = environmentsEditor;
        }

        /// <summary>
        /// Renders the list of remote environments
        /// </summary>
        public void Render()
        {
            if (fetchIsRunning && options.Count == 0)
            {
                EditorGUILayout.LabelField("Fetching remote options...", EditorStyles.helpBox);
            }
            else
            {
                // This is to stop Unity throwing an error.
                // Don't understand why it needs this, but we want to make sure the render doesn't have different layout is this case.
                EditorGUILayout.LabelField("", GUILayout.Height(0));
            }
            
            foreach (RemoteOption option in options)
            {
                if (option.Author?.Id != DeveloperProfile.Instance.userId) continue;
                RenderOption(option);
            }
        }

        private void RenderOption(RemoteOption option)
        {
            EditorGUILayout.LabelField(option.Name,  EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("Id",         option.Id);
            EditorGUILayout.LabelField("Thumbnail",  option.ThumbnailUrl);
            EditorGUILayout.LabelField("Is Private", option.IsPrivate.ToString());

            EnvironmentOption local = RenderLocalAssetField(option);

            if (!local) return;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Versions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical();
                    
            foreach (RemoteVersion version in option.Versions)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Version Code",       version.BundleVersion);
                EditorGUILayout.LabelField("Target App Version", version.AppVersion);
                
                SupportedPlatformsLabels(version);
                
                EditorGUILayout.EndVertical();
                
                if (GUILayout.Button("Edit Version"))
                {
                    UploadVersionWindow window = EditorWindow.GetWindow<UploadVersionWindow>();
                    window.environment = option;
                    window.localAsset = local;
                    window.existingVersion = version;
                    window.ShowUtility();
                    
                    environmentsEditor.FetchRemotes();
                    
                    // This might look bizarre, but ShowModalUtility locks this thread and executes further after the window is dismissed.
                    // If we do not return here the EndVertical is called during a later draw which then throws an error.
                    // A minor issue but the error message can confuse people otherwise.
                    return;
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Upload new Version"))
            {
                UploadVersionWindow window = EditorWindow.GetWindow<UploadVersionWindow>();
                window.environment = option;
                window.localAsset = local;
                window.ShowUtility();
                
                environmentsEditor.FetchRemotes();

                // This might look bizarre, but ShowModalUtility locks this thread and executes further after the window is dismissed.
                // If we do not return here the EndVertical is called during a later draw which then throws an error.
                // A minor issue but the error message can confuse people otherwise.
                return;
            }
                    
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        private EnvironmentOption RenderLocalAssetField(RemoteOption remote)
        {
            if (localOptions == null) return null;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Asset", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical();
            
            bool anyDirty = false;
            EnvironmentOption[] localAssets = localOptions.Where(opt => opt.databaseId == remote.Id)
                                                          .ToArray();

            EnvironmentOption option = localAssets.Length == 1 ? localAssets[0] : null;

            if (localAssets.Length == 1)
            {
                GUI.enabled = false;
                EditorGUILayout.ObjectField(option, typeof(EnvironmentOption), false);
                GUI.enabled = true;
            }
            else if (localAssets.Length == 0)
            {
                EnvironmentOption newOption = EditorGUILayout.ObjectField(option, typeof(EnvironmentOption), false) as EnvironmentOption;
                EditorGUILayout.LabelField("No local file found for this environment, create and assign an Environment Option to edit this environment", EditorStyles.helpBox);
            
                if (newOption && newOption != option)
                {
                    newOption.databaseId = remote.Id;
                    EditorUtility.SetDirty(newOption);
                }
                
                if (newOption != option)
                    anyDirty = true;
            }
            else
            {
                EditorGUILayout.LabelField("Multiple assets assigned to a single environment, only assign one asset to an environment.", EditorStyles.helpBox);
                foreach (EnvironmentOption currentOption in localAssets)
                {
                    EditorGUILayout.BeginHorizontal();

                    GUI.enabled = currentOption == null;
                    EnvironmentOption newOption = EditorGUILayout.ObjectField(currentOption, typeof(EnvironmentOption), false) as EnvironmentOption;
                    GUI.enabled = true;

                    if (GUILayout.Button("-", GUILayout.Width(24)) && currentOption != null)
                    {
                        currentOption.databaseId = null;
                        EditorUtility.SetDirty(currentOption);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    if (newOption == currentOption) continue;
                    if (newOption == null) continue;

                    newOption.databaseId = remote.Id;
                    
                    EditorUtility.SetDirty(newOption);
                    anyDirty = true;
                }
            }

            if (anyDirty) AssetDatabase.SaveAssets();
                                
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            return option;
        }
        
        private void SupportedPlatformsLabels(RemoteVersion version)
        {
            bool isFirst = true;

            void Append(string s)
            {
                string label = isFirst ? "Supported Platforms" : " ";
                EditorGUILayout.LabelField(label, s);
                isFirst = false;
            }

            foreach (Platform platform in Enum.GetValues(typeof(Platform)))
            {
                if (version[platform] != null)
                {
                    Append(Enum.GetName(typeof(Platform), platform));
                }
            }
        }
    }
}