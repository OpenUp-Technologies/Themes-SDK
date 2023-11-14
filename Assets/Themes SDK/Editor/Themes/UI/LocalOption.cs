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
    public class LocalOption
    {
        private readonly EnvironmentOption option;
        private readonly HashSet<EnvironmentOption> openEnvOptions;
        private readonly EnvironmentsEditor.Permissions permissions;
        private readonly HashSet<Platform> chosenPlatforms;

        public LocalOption(EnvironmentOption option, HashSet<EnvironmentOption> openEnvOptions,
                           EnvironmentsEditor.Permissions permissions, HashSet<Platform> chosenPlatforms)
        {
            this.option = option;
            this.openEnvOptions = openEnvOptions;
            this.permissions = permissions;
            this.chosenPlatforms = chosenPlatforms;
        }
        
        public void Render()
        {
            bool isOpen = EditorGUILayout.BeginFoldoutHeaderGroup(openEnvOptions.Contains(option), option.environmentName);

            if (!isOpen)
            {
                openEnvOptions.Remove(option);
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }
            
            openEnvOptions.Add(option);

            RenderBundleButton();
            RenderOrgDropdown();
            RenderPlatformSelection();
            
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Asset", option, typeof(EnvironmentOption), false);
            GUI.enabled = true;
            
            GameObject rootObject = EditorGUILayout.ObjectField("Root Object", option.rootObject.asset, typeof(GameObject), false) as GameObject;

            if (rootObject != option.rootObject.asset)
            {
                option.rootObject.asset = rootObject;
                EditorUtility.SetDirty(option);
            }
            
            Material skybox = EditorGUILayout.ObjectField("Sky box Object", option.skybox.asset, typeof(Material), false) as Material;
            
            if (skybox != option.skybox.asset)
            {
                option.skybox.asset = skybox;
                EditorUtility.SetDirty(option);
            }
            
            if (EditorUtility.IsDirty(option))
            {
                AssetDatabase.SaveAssets();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void RenderBundleButton()
        {
            if (GUILayout.Button("Upload Environment"))
            {
                CreateOption();
            }
        }

        private void RenderOrgDropdown()
        {
            EnvironmentsEditor.Organisation current = null;
            
            if (option.orgId != null) permissions.organisations.TryGetValue(option.orgId, out current); 
            
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
                        if (!chosenPlatforms.Contains(platform)) chosenPlatforms.Add(platform);
                        else chosenPlatforms.Remove(platform);
                    }

                    menu.AddItem(new GUIContent(platform.ToString()), chosenPlatforms.Contains(platform), ChosePlat, new object());
                }
                
                menu.ShowAsContext();
            }
        }

        private void SelectOrg(object orgId)
        {
            if (orgId is string id)
            {
                
                option.orgId = id;
            }
            else
            {
                option.orgId = null;
            }
            
            EditorUtility.SetDirty(option);
            AssetDatabase.SaveAssets();
        }

        private void CreateOption()
        {
            DeveloperProfile profile = DeveloperProfile.Instance;

            Task         tsk    = Task.Run(GetThing);
            RemoteOption newEnv = null;
            
            while (tsk.Status is not TaskStatus.RanToCompletion or TaskStatus.Faulted)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Processing", "Adding object to database", 0)) break;
                
                Thread.Sleep(32);
            }

            EditorUtility.ClearProgressBar();

            if (tsk.IsFaulted) throw new Exception("Failed to create new env", tsk.Exception);
            if (newEnv == null) throw new Exception("Failed to assign env");
            
            UploadVersion(newEnv);
            
            async Task GetThing()
            {
                SimpleApiCaller caller = await profile.GetSignedCaller();

                newEnv = await caller.PostAsync<RemoteOption>(
                             "environments", 
                             new { Name =  option.Name, IsPrivate = true, Thumbnail = "", OrganisationId = option.orgId }
                         );
            }
        }
        
        private void UploadVersion(RemoteOption newEnv)
        {
            DeveloperProfile profile = DeveloperProfile.Instance;

            Bundler bundler = new Bundler(option);
            Bundler.BundledEnv[] bundles = bundler.BuildBundles(chosenPlatforms.ToArray());

            Task.Run(() => DoAsyncUploadWork(profile, bundles, newEnv));
        }

        private async Task DoAsyncUploadWork(DeveloperProfile developerProfile, Bundler.BundledEnv[] bundles, RemoteOption remoteOption)
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