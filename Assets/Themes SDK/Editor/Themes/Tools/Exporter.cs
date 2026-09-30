using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using OpenUp.Environment;
using OpenUp.Interpreter.Environment;
using UnityEngine;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public class Exporter
    {
        public record Settings(string StorePath, HashSet<Platform> Platforms);
        public record Manifest(string SdkVersion, Dictionary<string, BundledItem> BundledItems, ThemedItem[] ThemedItems);
        
        public record BundledItem(string path, string hash, string fullPath);
        public record ThemedItem(string name);
        
        private readonly EnvironmentOption environmentOption;

        public Exporter(EnvironmentOption environmentOption)
        {
            this.environmentOption = environmentOption;
        }
        
        public void Run(Settings settings)
        {
            Bundler bundler = new Bundler(environmentOption);
            Bundler.BundledEnv[] output = bundler.BuildBundles(settings.Platforms.ToArray());
            
            Manifest manifest = new Manifest(
                Constants.SDK_VERSION,
                output.ToDictionary(x => x.platform.ToString(), x => new BundledItem(new FileInfo(x.path).Name, x.hash, x.path)),
                environmentOption.PrefabPaths.Select(x => new ThemedItem(x)).ToArray());
            
            string manifestJson = Newtonsoft.Json.JsonConvert.SerializeObject(manifest);
            
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