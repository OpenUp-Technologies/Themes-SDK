using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenUp.Interpreter.Environment;
using OpenUp.Networking;
using OpenUp.Networking.AWS;
using OpenUp.Networking.Editor;
using OpenUp.Utils;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public class EnvUploader
    {
        private class EnvBundleUrlSource : IUrlSource
        {
            private readonly string id;
            private readonly string version;
            private readonly SimpleApiCaller caller;
        
            public EnvBundleUrlSource(string id, string version, SimpleApiCaller caller)
            {
                this.id = id;
                this.version = version;
                this.caller = caller;
            }

            public async Task<UploadUrlResponse> GetUrl(string fileName)
            {
                return await caller.PostAsync<UploadUrlResponse>(
                    $"environments/{id}/upload_url",
                    new { Version = version, FileName = fileName });
            }
        }
        
        private readonly DeveloperProfile profile;
        private readonly RemoteOption environment;
        private readonly string version;

        public EnvUploader(DeveloperProfile profile, RemoteOption environment, string version)
        {
            this.profile     = profile;
            this.environment = environment;
            this.version = version;
        }
        
        /// <summary>
        /// Uploads all bundles to AWS.
        /// </summary>
         public async Task<Dictionary<Platform, RemoteBundle>> UploadAllBuildsAsync(Bundler.BundledEnv[] bundles)
        {
             Dictionary<Platform, RemoteBundle> uploaded = new Dictionary<Platform, RemoteBundle>();
             Dictionary<AWSDataSource, Bundler.BundledEnv> sourceForTarget = new Dictionary<AWSDataSource, Bundler.BundledEnv>();
        
             foreach (Bundler.BundledEnv bundle in bundles)
             {
                 sourceForTarget[new FileInfo(bundle.path)] = bundle;
                 sourceForTarget[new FileInfo(bundle.scenePath)] = bundle;
             }
             
             AWSDataSource[] files = sourceForTarget.Keys.ToArray();
             
             UploadTask task = new UploadTask("openup-environments", environment.Id, files);
        
             SimpleApiCaller caller = await profile.GetSignedCaller();
             var urlSource = new EnvBundleUrlSource(environment.Id, version, caller);
             
             // We want go past this call while it runs to display the progress bar
#pragma warning disable CS4014
             Task.Run(async () =>
#pragma warning restore CS4014
             {
                 try
                 {
                     await Uploader.UploadAsset(task, urlSource);
                 }
                 catch (Exception exception)
                 {
                     Debug.LogException(exception);
                 }
             });
             
             while (task.status is not (UploadTask.Status.FINISHED or UploadTask.Status.ERRORED))
             {
                 string humanReadableBytes = ToHumanReadableBytes(task.uploadedBytes, task.totalBytes);

                 await AsyncHelperEditor.DoUnityWork(() => EditorUtility.DisplayProgressBar(
                     "Uploading Theme",
                     $"State = {task.status}, Uploaded {humanReadableBytes}",
                     task.uploadedBytes / (float)task.totalBytes));
                 
                 await Task.Delay(100);
             }
        
             await AsyncHelperEditor.DoUnityWork(EditorUtility.ClearProgressBar);
        
             for (int i = 0; i < files.Length; i+=2)
             {
                 Bundler.BundledEnv bundl = sourceForTarget[files[i]];

                 uploaded[bundl.platform] =
                     new RemoteBundle
                     {
                         BundleUrl = $@"https://openup-environments.s3.eu-central-1.amazonaws.com/{environment.Id}/{task.files[i].name}",
                         BundleHash = bundl.hash,
                         SceneBundleUrl = $@"https://openup-environments.s3.eu-central-1.amazonaws.com/{environment.Id}/{task.files[i+1].name}",
                         SceneBundleHash = bundl.sceneHash,
                     };
             }

             return uploaded;
        }

        private string ToHumanReadableBytes(long uploaded, long total)
        {
            string[]  names = { "B", "KB", "MB", "GB" };
            int       idx   = 0;
            const int step  = 1024;


            while (total >  Mathf.Pow(step, idx+1))
            {
                idx++;
            }

            float tot = total    / Mathf.Pow(step, idx);
            float up  = uploaded / Mathf.Pow(step, idx);

            return $"{up:F1} of {tot:F1} {names[idx]}";
        }

        public async Task ConfirmUploadOf(RemoteVersion newVersion, string[] prefabPaths, Dictionary<Platform, RemoteBundle> uploaded)
        {
            SimpleApiCaller caller = await profile.GetSignedCaller();
            
            newVersion.UsedScripts = Array.Empty<string>();
            newVersion.PrefabPaths = prefabPaths;

            foreach ((Platform platform, RemoteBundle bundle) in uploaded)
            {
                newVersion[platform] = bundle;
            }

            await caller.PutAsync($"environments/{environment.Id}/version", newVersion);
        }
    }
}