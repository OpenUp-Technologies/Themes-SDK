using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OpenUp.Environment;
using OpenUp.Interpreter.Environment;
using OpenUp.Interpreter.Utils;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public class Bundler
    {
        public record BundledEnv(Platform platform, BuildTarget target)
        {
            public string path;
            public string hash;
        }

        public record ValidationResult(bool IsValid, IEnumerable<string> Reasons)
        {
            public ValidationResult Combine(ValidationResult other) =>
                new ValidationResult(IsValid && other.IsValid, Reasons.Concat(other.Reasons));
            
            public ValidationResult Combine(bool isValid, string reason) =>
                new ValidationResult(IsValid && isValid, Reasons.Append(reason));
        };

     
        private static readonly IReadOnlyDictionary<Platform, BuildTarget> TargetPlatformMap =
            new Dictionary<Platform, BuildTarget>
            {
                [Platform.OCULUS]         = BuildTarget.Android,
                [Platform.ANDROID]        = BuildTarget.Android,
                [Platform.IOS]            = BuildTarget.iOS,
                [Platform.EDITOR]         = BuildTarget.StandaloneWindows64,
                [Platform.STANDALONE]     = BuildTarget.StandaloneWindows64,
                [Platform.STANDALONE_MAC] = BuildTarget.StandaloneOSX,
                [Platform.EDITOR_MAC]     = BuildTarget.StandaloneOSX,
                [Platform.HOLOLENS]       = BuildTarget.WSAPlayer
            };
        
        private readonly EnvironmentOption option;
        
        public Bundler(EnvironmentOption option)
        {
            this.option = option;
        }

        /// <summary>
        /// Checks whether the output of this bundle could be used by the target version. 
        /// </summary>
        /// <param name="targetVersion"></param>
        /// <returns></returns>
        public ValidationResult Validate(RemoteVersion targetVersion)
        {
            ValidationResult result = new ValidationResult(true, Array.Empty<string>());

            if (!option.rootObject.isSet || option.rootObject.isBroken)
                result = result.Combine(false, "Root object is missing");

            if (targetVersion != null)
                result = result.Combine(ValidateForTargetVersion(targetVersion));
            
            return result;
        }

        private ValidationResult ValidateForTargetVersion(RemoteVersion targetVersion)
        {
            ValidationResult result = new ValidationResult(true, Array.Empty<string>());
            string[] ourPaths = option.prefabs.Select(AssetDatabase.GetAssetPath).ToArray();
            
            foreach (string path in ourPaths)
            {
                if (!targetVersion.PrefabPaths.Contains(path))
                    result = result.Combine(false, $"Prefab {path} is not part of the remote prefabs list");
            }
            
            foreach (string path in targetVersion.PrefabPaths)
            {
                if (!ourPaths.Contains(path))
                    result = result.Combine(false, $"Missing prefab for remotely listed object {path}");
            }

            return result;
        }

        private AssetBundleBuild CreateBuildInstructions()
        {
            string           bundleName = $"world_{option.GetInstanceID()}";
            string rootPath = AssetDatabase.GetAssetPath(option.rootObject.asset.GetInstanceID());


            string rootPathAR = option.rootObjectAR.isSet
                ? AssetDatabase.GetAssetPath(option.rootObjectAR.asset.GetInstanceID())
                : rootPath;
            
            Regex dontWants = new Regex(@"\.cs|\.dll");

            IEnumerable<string> allFiles = AssetDatabase.GetDependencies(rootPath, true)
                                                        .Concat(AssetDatabase.GetDependencies(rootPathAR, true))
                                                        .Where(d => !dontWants.IsMatch(d))
                                                        .Append(rootPath)
                                                        .Append(rootPathAR)
                                                        .Distinct();

            string skyboxPath = null;
            
            if (option.skybox.asset != null)
            {
                skyboxPath = AssetDatabase.GetAssetPath(option.skybox.asset.GetInstanceID());
                allFiles   = allFiles.Append(skyboxPath);
            }

            IEnumerable<string> prefabPaths = option.prefabs.Select(AssetDatabase.GetAssetPath);

            foreach (string prefabPath in prefabPaths)
            {
                string[] prefabFiles = AssetDatabase.GetDependencies(prefabPath, true)
                                                    .Where(d => !dontWants.IsMatch(d))
                                                    .Append(prefabPath)
                                                    .ToArray();
                
                if (!prefabFiles.Any()) throw new FileNotFoundException($"Prefab at path {prefabPath} not found");
                
                allFiles = allFiles.Concat(prefabFiles);
            }
            
            // Playable object can reference prefabs not included yet
            allFiles = allFiles.Concat(option.rootObject.asset.GetComponentsInChildren<PlayableObject>().Select(play => play.source));
            
            if (option.rootObjectAR.isSet) 
                allFiles = allFiles.Concat(option.rootObjectAR.asset.GetComponentsInChildren<PlayableObject>().Select(play => play.source));
            
            string[] deps = allFiles.Distinct()
                                    .ToArray();

            string[] names = new string[deps.Length];
            
            for (int i = 0; i < deps.Length; i++)
            {
                if (deps[i] == rootPath) names[i] = EnvironmentOption.ROOT_OBJECT_NAME;
                else if (deps[i] == rootPathAR) names[i] = EnvironmentOption.ROOT_OBJECT_NAME_AR;
                else if (deps[i] == skyboxPath) names[i] = EnvironmentOption.SKYBOX_NAME;

                else names[i] = deps[i];
            }

            return new AssetBundleBuild
            {
                assetBundleName = bundleName,
                assetNames = deps,
                addressableNames = names
            };
        }

        public BundledEnv[] BuildBundles(AssetBundleBuild build, Platform[] platforms)
        {
            string targetFolder = $"{Application.temporaryCachePath}/env_bundles";

            BundledEnv[] bundles = platforms.Select(plat => new BundledEnv(plat, TargetPlatformMap[plat]))
                                          .ToArray();

            // for each unique build target
            foreach (BuildTarget target in bundles.Select(b => b.target).Distinct())
            {
                AssetBundleBuild targetBuild = build;
                
                targetBuild.assetBundleName = $"{build.assetBundleName}_{target}";
                
                string subFolder = $"{targetFolder}/{target}";

                Directory.CreateDirectory(subFolder);
                
                AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                    subFolder, 
                    new[] { targetBuild },
                    BuildAssetBundleOptions.StrictMode, 
                    target
                );

                IEnumerable<BundledEnv> targetedPlatforms = bundles.Where(b => b.target == target);
                
                // assign the built bundle to all platforms that use this target.
                foreach (BundledEnv bundledEnv in targetedPlatforms)
                {
                    bundledEnv.path = $"{subFolder}/{targetBuild.assetBundleName}";
                    bundledEnv.hash = manifest.GetAssetBundleHash(targetBuild.assetBundleName)
                                              .ToString();
                }
            }

            return bundles;
        }

        public BundledEnv[] BuildBundles(Platform[] targets)
        {
            return BuildBundles(CreateBuildInstructions(), targets);
        }
    }
}