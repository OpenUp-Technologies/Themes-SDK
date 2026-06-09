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
using Object = UnityEngine.Object;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public class ImageRatio
    {
        /// <summary>
        /// Width over height, e.g. 16/9. Bigger number => wider picture.
        /// </summary>
        public float ratio => width / (float) height;
        private readonly int width;
        private readonly int height;
        
        public ImageRatio(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public bool Validate(Texture2D target)
        {
            if (ratio == 0) return true;
            
            float currentRatio = target.width / (float)target.height;

            return Mathf.Abs(currentRatio - ratio) < 0.01f;
        }
        
        public override string ToString() => $"({width} : {height})";
        
        public static ImageRatio Any => new ImageRatio(0, 1);
        public static ImageRatio Square => new ImageRatio(1, 1);
        public static ImageRatio Landscape => new ImageRatio(16, 9);
        public static ImageRatio Portrait => new ImageRatio(9, 16);
    }
    
    public class ImageUploadWindow : EditorWindow
    {
        private string preexistingUrl = "";
        private Action<string> callback;
        private UnityWebRequest previewRequest;
        private Texture2D previewTexture;
        private bool requiresResize = false;

        /// <summary>
        /// Required to create a url for the thumbnail.
        /// </summary>
        public IUrlSource urlSource;
        public ImageRatio imageRatio = ImageRatio.Any;

        private void OnEnable()
        {
            titleContent = new GUIContent("Choose or upload an image");
        }
        
        private void OnGUI()
        {
            if (GUILayout.Button("Choose from disk"))
            {
                string path = EditorUtility.OpenFilePanel("Choose an image", "", "png");

                if (path.Length == 0) return;
                
                preexistingUrl = "";
                Texture2D tex = ValidateFile(path);
                
                if (!requiresResize) 
                    Task.Run(UploadToCloud(path));
                else
                    previewTexture = Crop(tex);

                return;
            }

            string oldUrl = preexistingUrl;
            preexistingUrl = EditorGUILayout.TextField("Preexisting Url", preexistingUrl);
            
            if (oldUrl != preexistingUrl)
                UpdatePreview(preexistingUrl);
            
            if (previewTexture)
            {
                GUILayout.Box(previewTexture, 
                    GUILayout.Width(400), 
                    GUILayout.Height(previewTexture.height * 400 / previewTexture.width), 
                    GUILayout.ExpandWidth(true));
            }
            
            if (requiresResize)
                EditorGUILayout.HelpBox("This image will be resized to fit the aspect ratio of " + imageRatio, MessageType.Warning);

            if (GUILayout.Button("Confirm"))
            {
                if (requiresResize)
                    Task.Run(UploadToCloud(previewTexture.EncodeToPNG()));
                else
                {
                    callback.Invoke(preexistingUrl);
                    Close();
                }
            }
        }

        public void Setup(Action<string> callback, ImageRatio ratio = null)
        {
            this.callback = callback ?? throw new ArgumentNullException(nameof(callback));

            imageRatio = ratio ?? ImageRatio.Any;
            previewRequest = null;

            Show();
        }

        private Texture2D ValidateFile(string path)
        {
            if (!File.Exists(path)) 
                throw new FileNotFoundException("File not found", path);
            
            Texture2D target = new Texture2D(1, 1);
            target.LoadImage(File.ReadAllBytes(path));

            requiresResize = !imageRatio.Validate(target);

            return target;
        }
        
        private Texture2D Crop(Texture2D target)
        {
            if (imageRatio.ratio == 0)
                throw new ArgumentException("Cannot crop if we don't have a specific ratio");
            
            (int width, int height) = (target.width, target.height);
                
            float currentRatio = width / (float)height;
            
            int newWidth = width;
            int newHeight = height;

            if (currentRatio > imageRatio.ratio)
            {
                newWidth = Mathf.RoundToInt(height * imageRatio.ratio);
            }
            else
            {
                newHeight = Mathf.RoundToInt(width / imageRatio.ratio);
            }

            int x = (width - newWidth) / 2;
            int y = (height - newHeight) / 2;

            Color[] pixels = target.GetPixels(x, y, newWidth, newHeight);
            Texture2D newTexture = new Texture2D(newWidth, newHeight);
            newTexture.SetPixels(pixels);
            newTexture.Apply();

            return newTexture;
        }

        private void UpdatePreview(string preview)
        {
            if (previewTexture) Object.Destroy(previewTexture);
            
            previewTexture = null;
            
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
            previewRequest.SendWebRequest()
                          .completed += OnDownload;
        }

        private void OnDownload(AsyncOperation _)
        {
            previewTexture = ((DownloadHandlerTexture)previewRequest.downloadHandler).texture;
            
            requiresResize = !imageRatio.Validate(previewTexture);
            
            if (requiresResize)
                previewTexture = Crop(previewTexture);
        }

        private Func<Task> UploadToCloud(string filePath) => UploadToCloud(new FileInfo(filePath));
        private Func<Task> UploadToCloud(byte[] data) => UploadToCloud(new AWSDataSource { data = data });
        
        private Func<Task> UploadToCloud(AWSDataSource source) => async () =>
        {
            try
            {
                if (urlSource == null)
                    throw new ArgumentNullException(nameof(urlSource));
                
                string id = Guid.NewGuid() + ".png";
                source.name = id;

                UploadTask uploadTask = new UploadTask(source);
                await Uploader.UploadAsset(
                    uploadTask,
                    urlSource);

                await AsyncHelperEditor.DoUnityWork(() =>
                {
                    string url = uploadTask.uploadLinks[source].ResourceUrl;
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