using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using OpenUp.Environment;
using OpenUp.Interpreter.Environment;
using UnityEngine;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public class Exporter
    {
        public enum ExportType
        {
            ObjectsOnly,
            Scene,
            RootObject,
            All
        }
        
        public record Settings(string StorePath, bool ObjectsOnly, HashSet<Platform> Platforms);
        public record Manifest(string SdkVersion, ExportType Type, Dictionary<string, BundledItem> BundledItems, ThemedItem[] ThemedItems);

        public record BundledItem(string path, string hash, string fullPath)
        {
            public static BundledItem Create(string path, string hash)
            {
                return new BundledItem(new FileInfo(path).Name, hash, path);
            }
        }
        public record ThemedItem(string name);
        
        private readonly EnvironmentOption environmentOption;

        public Exporter(EnvironmentOption environmentOption)
        {
            this.environmentOption = environmentOption;
        }
        
        public void Run(Settings settings)
        {
            Bundler bundler = new Bundler(environmentOption);
            Bundler.BundledEnv[] output = bundler.BuildBundles(settings.Platforms.ToArray(), settings.ObjectsOnly);
            
            Dictionary<string, BundledItem> bundledItems = new Dictionary<string, BundledItem>();

            foreach (Bundler.BundledEnv bundle in output)
            {
                if (settings.ObjectsOnly)
                {
                    bundledItems.Add($"{bundle.platform}_objects", BundledItem.Create(bundle.objectsPath, bundle.objectsHash));
                }
                else
                {
                    bundledItems.Add($"{bundle.platform}_assets", BundledItem.Create(bundle.path, bundle.hash));
                    bundledItems.Add($"{bundle.platform}_scene", BundledItem.Create(bundle.scenePath, bundle.sceneHash));
                    bundledItems.Add($"{bundle.platform}_objects", BundledItem.Create(bundle.objectsPath, bundle.objectsHash));
                }
            }
            
            ExportType type = environmentOption.Supports switch
            {
                _ when settings.ObjectsOnly => ExportType.ObjectsOnly,
                EnvironmentOption.ModeSupport.Both => ExportType.All,
                EnvironmentOption.ModeSupport.Scene => ExportType.Scene,
                EnvironmentOption.ModeSupport.RootObject => ExportType.RootObject,
                _ => throw new ArgumentException("Invalid mode support")
            };
            
            Manifest manifest = new Manifest(
                Constants.SDK_VERSION, 
                type, 
                bundledItems,
                environmentOption.PrefabPaths.Select(x => new ThemedItem(x)).ToArray());
            
            string manifestJson = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            string targetFolder = $"{Application.temporaryCachePath}/env_bundles";
            string manifestPath = $"{targetFolder}/manifest.json";
            
            File.WriteAllText(manifestPath, manifestJson);
            
            ZipArchive zipArchive = new ZipArchive(File.Create(settings.StorePath), ZipArchiveMode.Create);
            
            foreach (string file in manifest.BundledItems.Values.Select(x => x.fullPath).Distinct())
                AddFile(file, zipArchive);
            
            AddFile(manifestPath, zipArchive);

            zipArchive.Dispose();
        }

        private void AddFile(string filePath, ZipArchive archive)
        {
            FileInfo info = new FileInfo(filePath);
            ZipArchiveEntry zipEntry = archive.CreateEntry(info.Name);
                
            using Stream zipStream = zipEntry.Open();
            using FileStream fileStream = File.OpenRead(filePath);
                
            fileStream.CopyTo(zipStream);
        }
    }
}