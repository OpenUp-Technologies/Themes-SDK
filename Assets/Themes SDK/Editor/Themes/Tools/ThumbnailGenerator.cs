using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public static class ThumbnailGenerator
    {
        public const string ThumbnailPrepend = "_Thumbnail_";

        public static string GenerateForPrefab(GameObject target)
        {
            var gos = PrefabUtility.FindAllInstancesOfPrefab(target);
            
            if (gos.Length == 0)
                throw new Exception("Can only generate thumbnails for prefabs that are currently instantiated in the scene");

            var existing = Object.FindFirstObjectByType<ThumbnailCam>();
            
            if (existing == null)
                existing = CreateDefault();

            return StoreTexture(existing.GenerateThumbnail(gos[0]));
        }

        private static ThumbnailCam CreateDefault()
        {
            GameObject go = new GameObject("Thumbnail Generator");
            go.AddComponent<Camera>();
            go.AddComponent<ThumbnailCam>();
            return go.GetComponent<ThumbnailCam>();
        }

        public static void StoreImage(ThumbnailCam cam)
        {
            StoreTexture(cam.GenerateThumbnail(null));
        }

        private static string StoreTexture(RenderTexture tex)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = tex;
            
            Texture2D readableText = new Texture2D(tex.width, tex.height);
            
            readableText.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            readableText.Apply();
            
            RenderTexture.active = prev;
        
            byte[] imageData = readableText.EncodeToPNG();
            
            string path = $"{Application.dataPath}/Generated/Thumbnails/_Thumbnail_{Guid.NewGuid().ToString()}.png";
            StoreThumbnail(imageData, path);
            
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(readableText);
            
            return path;
        }

        private static void StoreThumbnail(byte[] png, string path)
        {
            Directory.CreateDirectory($"{Application.dataPath}/Generated/Thumbnails");
            
            File.WriteAllBytes(path, png);
        }
    }
}