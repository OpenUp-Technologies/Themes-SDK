using System;
using System.IO;
using System.Threading.Tasks;
using OpenUp.Networking;
using OpenUp.Networking.AWS;
using OpenUp.Networking.Editor;
using OpenUp.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public class ImageUploadWindow : EditorWindow
    {
        private string preexistingUrl;
        private Action<string> callback;
        private UnityWebRequest previewRequest;

        private void OnEnable()
        {
            titleContent = new GUIContent("Choose or upload an image");
        }
        
        private void OnGUI()
        {
            if (GUILayout.Button("Choose from disk"))
            {
                string path = EditorUtility.OpenFilePanel("Choose an image", "", "png");
                
                if (path.Length > 0)
                {
                    Task.Run(UploadToCloud(DeveloperProfile.Instance, path));
                }

                return;
            }

            string oldUrl = preexistingUrl;
            preexistingUrl = EditorGUILayout.TextField("Preexisting Url", preexistingUrl);
            
            if (oldUrl != preexistingUrl)
                UpdatePreview(preexistingUrl);
            
            if (previewRequest is { isDone: true, result: UnityWebRequest.Result.Success })
            {
                DownloadHandlerTexture tex = previewRequest.downloadHandler as DownloadHandlerTexture;
                GUILayout.Box(tex!.texture, GUILayout.Width(400), GUILayout.Height(tex.texture.height * 400 / tex.texture.width), GUILayout.ExpandWidth(true));
            }

            if (GUILayout.Button("Confirm"))
            {
                callback.Invoke(preexistingUrl);
                Close();
            }
        }

        public void Setup(Action<string> callback)
        {
            this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
            previewRequest = null;
            Show();
        }

        private void UpdatePreview(string preview)
        {
            if (previewRequest is { isDone: false })
            {
                previewRequest.Abort();
                previewRequest.Dispose();
            }
            else if (previewRequest is { isDone: true })
            {
                previewRequest.Dispose();
            }
            
            previewRequest = UnityWebRequestTexture.GetTexture(preview);
            previewRequest.SendWebRequest();
        }

        private Func<Task> UploadToCloud(DeveloperProfile profile, string filePath) => async () =>
        {
            try
            {
                SimpleApiCaller caller = await profile.GetSignedCaller();
                AWSDataSource source = new FileInfo(filePath);
                
                string id = Guid.NewGuid().ToString() + ".png";
                source.name = id;
                
                await Uploader.UploadAsset(
                    new UploadTask("openup-environments", "thumbnails", id, source),
                    caller);

                await AsyncHelperEditor.DoUnityWork(() =>
                {
                    string url = $@"https://openup-environments.s3.eu-central-1.amazonaws.com/thumbnails/{id}";
                    callback.Invoke(url);
                    Close();
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        };
    }
}