using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenUp.Environment;
using OpenUp.Interpreter.Environment;
using OpenUp.Networking;
using OpenUp.Networking.Editor;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public class CreateEnvUI : EditorWindow
    {
        private readonly HashSet<Platform> chosenPlatforms = new HashSet<Platform>();
        private string envName = "";
        private GameObject rootPrefab = null;
        private Material skybox = null;
        private string organisation = "";

        private EnvironmentsEditor.Permissions permissions => EnvironmentsEditor.Instance.permissions;
        
        private void OnEnable()
        {
            envName = "";
            rootPrefab = null;
            skybox = null;
            titleContent = new GUIContent("New Theme");
        }

        private void OnGUI()
        {
            envName = EditorGUILayout.TextField("Theme Name", envName);
            rootPrefab = EditorGUILayout.ObjectField("Root Prefab", rootPrefab, typeof(GameObject), false) as GameObject;
            skybox = EditorGUILayout.ObjectField("Skybox", skybox, typeof(Material), false) as Material;
            
            RenderOrgDropdown();
            RenderPlatformSelection();
            
            if (GUILayout.Button("Create Theme"))
            {
                Create();
                Close();
            }
        }

        private void Create()
        {
            if (String.IsNullOrWhiteSpace(envName)) throw new ArgumentException("Must give the theme a valid name.");
            if (rootPrefab == null) throw new ArgumentException("Must assign a root prefab for the theme");

            EnvironmentOption option = ScriptableObject.CreateInstance<EnvironmentOption>();
            option.name = envName;
            option.environmentName = envName;
            option.rootObject = rootPrefab;
            
            CreateOption(option);
        } 
        
        private void RenderOrgDropdown()
        {
            EnvironmentsEditor.Organisation current = null;
            
            if (organisation != null) permissions.organisations?.TryGetValue(organisation, out current); 
            
            if (GUILayout.Button(current?.orgName ?? "Set an organisation"))
            {
                GenericMenu menu = new GenericMenu();
                
                menu.AddItem(new GUIContent("No Org"), current == null, SelectOrg, null);
                
                foreach (EnvironmentsEditor.Organisation org in permissions.organisations.Values)
                {
                    menu.AddItem(new GUIContent(org.orgName), current?.orgId == org.orgId, SelectOrg, org.orgId);
                }
                
                menu.ShowAsContext();
            }
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
                        if (!chosenPlatforms.Contains(platform)) 
                            chosenPlatforms.Add(platform);
                        else 
                            chosenPlatforms.Remove(platform);
                    }

                    menu.AddItem(new GUIContent(platform.ToString()), chosenPlatforms.Contains(platform), ChosePlat, new object());
                }
                
                menu.ShowAsContext();
            }
        }
        
        private void SelectOrg(object orgId)
        {
            if (orgId is string id)
                organisation = id;
            else
                organisation = null;
        }

        private void CreateOption(EnvironmentOption option)
        {
            DeveloperProfile profile = DeveloperProfile.Instance;

            RemoteOption newEnv = null;
            
            async Task GetThing()
            {
                SimpleApiCaller caller = await profile.GetSignedCaller();

                newEnv = await caller.PostAsync<RemoteOption>(
                    "environments", 
                    new { Name =  option.Name, IsPrivate = true, Thumbnail = "", OrganisationId = organisation }
                );
            }

            Task tsk = Task.Run(GetThing);
            
            while (tsk.Status is not (TaskStatus.RanToCompletion or TaskStatus.Faulted))
            {
                if (EditorUtility.DisplayCancelableProgressBar("Processing", "Adding object to database", 0)) break;
                
                Thread.Sleep(32);
            }

            EditorUtility.ClearProgressBar();

            if (tsk.IsFaulted) throw new Exception("Failed to create new env", tsk.Exception);
            if (newEnv == null) throw new Exception("Failed to assign env");

            option.databaseId = newEnv.Id;
            
            string path = EditorUtility.SaveFilePanelInProject("Save Environment", envName, "asset",
                "Choose a location to save your environment.");

            if (String.IsNullOrEmpty(path))
            {
                Debug.LogError("Canceled creation of environment.");
            }
            else
            {
                AssetDatabase.CreateAsset(option, path);
                AssetDatabase.SaveAssets();
            }
            
            UploadVersion(newEnv, option);
        }
        
        private void UploadVersion(RemoteOption newEnv, EnvironmentOption option)
        {
            DeveloperProfile profile = DeveloperProfile.Instance;

            Bundler bundler = new Bundler(option);
            Bundler.BundledEnv[] bundles = bundler.BuildBundles(chosenPlatforms.ToArray());

            Task.Run(() => DoAsyncUploadWork(profile, bundles, newEnv, option));
        }

        private async Task DoAsyncUploadWork(DeveloperProfile developerProfile, Bundler.BundledEnv[] bundles, RemoteOption remoteOption, EnvironmentOption option)
        {
            try
            {
                EnvUploader uploader = new EnvUploader(developerProfile, remoteOption);
                
                Dictionary<Platform, RemoteBundle> uploaded = await uploader.UploadAllBuildsAsync(bundles);
                
                RemoteVersion newVersion = new RemoteVersion { BundleVersion = "0.1", AppVersion = EnvironmentsSettingsWindow.APP_VERSION };
                
                await uploader.ConfirmUploadOf(newVersion, option.prefabPaths, uploaded);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}