using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OpenUp.Environment;
using OpenUp.Environment.Features;
using OpenUp.Interpreter.Environment;
using OpenUp.Interpreter.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public class Bundler
    {
        public record BundledEnv(Platform platform, BuildTarget target)
        {
            public string path;
            public string hash;
            public string scenePath;
            public string sceneHash;
            public string objectsPath;
            public string objectsHash;
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
        private readonly Regex dontWants = new Regex(@"\.cs|\.dll");

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
            string[] ourPaths = option.themedObjects is { Length: > 0 } ? 
                option.themedObjects.Select(obj => AssetDatabase.GetAssetPath(obj.GameObject)).ToArray() :
                option.prefabs.Select(AssetDatabase.GetAssetPath).ToArray();
            
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

        // Creates a StartingState scriptable object and returns the path to it.
        private string CreateStartingStateObject()
        {
            StartingState startingState = ScriptableObject.CreateInstance<StartingState>();

            if (option.scene != null)
            {
                string scenePath = AssetDatabase.GetAssetPath(option.scene);
                Scene scene = SceneManager.GetSceneByPath(scenePath);

                if (!scene.isLoaded)
                    EditorSceneManager.OpenScene(scenePath);

                var roots = scene.GetRootGameObjects();
                startingState.SetStartingState(roots);
            }
            else if (option.rootObject.isSet)
            {
                startingState.SetStartingState(option.rootObject.asset);
            }

            string tempPath = @"Assets/Generated/StartingState.asset";
            
            AssetDatabase.CreateAsset(startingState, tempPath);
            return tempPath;
        }

        /// <summary>
        /// This bundle contains all objects that have been prepared and can be added to world by the player
        /// </summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        private AssetBundleBuild CreateThemedObjectsBundle(ThemedObject[] objects)
        {
            IEnumerable<string> allFiles = Array.Empty<string>();
            IEnumerable<string> prefabPaths = objects.Select(obj => AssetDatabase.GetAssetPath(obj.GameObject));

            foreach (string prefabPath in prefabPaths)
            {
                string[] prefabFiles = AssetDatabase.GetDependencies(prefabPath, true)
                                                    .Where(d => !dontWants.IsMatch(d))
                                                    .Append(prefabPath)
                                                    .ToArray();
                
                if (!prefabFiles.Any()) throw new FileNotFoundException($"Prefab at path {prefabPath} not found");
                
                allFiles = allFiles.Concat(prefabFiles);
            }
            
            allFiles = allFiles.Distinct();
            
            ThemedObjectsList list = ScriptableObject.CreateInstance<ThemedObjectsList>();
            list.ThemedObjects = objects;
            
            string tempPath = @"Assets/Generated/ThemedObjectsList.asset";
            AssetDatabase.CreateAsset(list, tempPath);
            
            allFiles = allFiles.Prepend(tempPath);
            string[] names = allFiles.ToArray();
            names[0] = EnvironmentOption.THEMED_OBJECTS_NAME;
            
            return new AssetBundleBuild
            {
                assetNames = allFiles.ToArray(),
                addressableNames = names
            };
        }

        /// <summary>
        /// Contains everything apart from the scene needed to create the world.
        /// </summary>
        /// <param name="dontInclude">Items to omit, this is used to make sure the themed objects are not bundled in twice</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        private AssetBundleBuild CreateRootPrefabBundle(string[] dontInclude)
        {
            IEnumerable<string> allFiles = new string[0];

            string rootPath = null;
            string rootPathAR = null;
            
            if (option.scene)
                allFiles = allFiles.Concat(AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(option.scene.GetInstanceID()), true));
            
            if (option.rootObject.isSet)
            {
                rootPath = AssetDatabase.GetAssetPath(option.rootObject.asset.GetInstanceID());
                
                allFiles = allFiles.Concat(AssetDatabase.GetDependencies(rootPath, true))
                                   .Append(rootPath);
            }

            if (option.rootObjectAR.isSet)
            {
                rootPathAR = AssetDatabase.GetAssetPath(option.rootObjectAR.asset.GetInstanceID());
                
                allFiles = allFiles.Concat(AssetDatabase.GetDependencies(rootPathAR, true))
                                   .Append(rootPathAR);
            }

            allFiles = allFiles.Where(d => !dontWants.IsMatch(d))
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
            if (option.rootObject.isSet)
                allFiles = allFiles.Concat(option.rootObject.asset.GetComponentsInChildren<PlayableObject>().Select(play => play.source));
            
            if (option.rootObjectAR.isSet) 
                allFiles = allFiles.Concat(option.rootObjectAR.asset.GetComponentsInChildren<PlayableObject>().Select(play => play.source));

            if (option.scene)
                allFiles = allFiles.Concat(EditorSceneManager.GetActiveScene().GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<PlayableObject>()).Select(play => play.source));

            string startingStatePath = CreateStartingStateObject();

            allFiles = allFiles.Append(startingStatePath);
            allFiles = allFiles.Where(path => !dontInclude.Contains(path));
            
            string[] deps = allFiles.Distinct()
                                    .ToArray();
            
            string[] names = new string[deps.Length];
            
            for (int i = 0; i < deps.Length; i++)
            {
                if (deps[i] == rootPath) 
                    names[i] = EnvironmentOption.ROOT_OBJECT_NAME;
                else if (deps[i] == rootPathAR) 
                    names[i] = EnvironmentOption.ROOT_OBJECT_NAME_AR;
                else if (deps[i] == skyboxPath) 
                    names[i] = EnvironmentOption.SKYBOX_NAME;
                else if (deps[i] == startingStatePath)
                    names[i] = EnvironmentOption.STARTING_STATE_NAME;

                else names[i] = deps[i];
            }
            
            return new AssetBundleBuild
            {
                assetNames = deps,
                addressableNames = names
            };
        }

        private AssetBundleBuild CreateSceneBundle()
        {
            if (option.scene == null)
                return new AssetBundleBuild();
            
            string scenePath = AssetDatabase.GetAssetPath(option.scene.GetInstanceID());
            
            return new AssetBundleBuild
            {
                assetNames = new string[] { scenePath },
                addressableNames = new string[] { EnvironmentOption.SCENE_NAME }
            };
        }

        private (AssetBundleBuild rootObject, AssetBundleBuild scene, AssetBundleBuild objects) CreateBuildInstructions()
        {
            if (!Directory.Exists(Application.dataPath.Replace("Assets", "Assets/Generated")))
                Directory.CreateDirectory(Application.dataPath.Replace("Assets", "Assets/Generated"));

            string scenePath = "scene_thing";
            
            if (option.scene )
            {
                scenePath = AssetDatabase.GetAssetPath(option.scene.GetInstanceID());
                string activeScene = EditorSceneManager.GetActiveScene().path;
                
                if (activeScene != scenePath)
                    EditorSceneManager.OpenScene(scenePath);
            }
            
            string bundleName = $"world_{option.GetInstanceID()}";

            var objects = CreateThemedObjectsBundle(option.themedObjects);
            var rootObject = CreateRootPrefabBundle(objects.assetNames.Append(scenePath).ToArray());
            var scene = CreateSceneBundle();
            
            objects.assetBundleName = bundleName+"_objects";
            rootObject.assetBundleName = bundleName+"_assets";
            scene.assetBundleName = bundleName+"_scene";
            
            return (rootObject, scene, objects);
        }

        private BundledEnv[] BuildBundles(
            (AssetBundleBuild rootObject, AssetBundleBuild scene, AssetBundleBuild objects) build, 
            Platform[] platforms , 
            bool objectsOnly = false)
        {
            string targetFolder = $"{Application.temporaryCachePath}/env_bundles";

            BundledEnv[] bundles = platforms.Select(plat => new BundledEnv(plat, TargetPlatformMap[plat]))
                                          .ToArray();

            // for each unique build target
            foreach (BuildTarget target in bundles.Select(b => b.target).Distinct())
            {
                AssetBundleBuild buildPrefabs = build.rootObject;
                AssetBundleBuild buildScene = build.scene;
                AssetBundleBuild buildObjects = build.objects;
                
                buildPrefabs.assetBundleName = $"{build.rootObject.assetBundleName}_{target}";
                buildScene.assetBundleName = $"{build.scene.assetBundleName}_{target}";
                buildObjects.assetBundleName = $"{build.objects.assetBundleName}_{target}";
                
                string subFolder = $"{targetFolder}/{target}";

                Directory.CreateDirectory(subFolder);
                
                AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                    subFolder, 
                    objectsOnly ? new[] { buildObjects } : new[] { buildPrefabs, buildScene, buildObjects },
                    BuildAssetBundleOptions.StrictMode
                        | BuildAssetBundleOptions.DisableLoadAssetByFileName
                        | BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension
                        ,
                    target
                );

                IEnumerable<BundledEnv> targetedPlatforms = bundles.Where(b => b.target == target);
                
                // assign the built bundle to all platforms that use this target.
                foreach (BundledEnv bundledEnv in targetedPlatforms)
                {
                    bundledEnv.objectsPath = $"{subFolder}/{buildObjects.assetBundleName}";
                    bundledEnv.objectsHash = manifest.GetAssetBundleHash(buildObjects.assetBundleName).ToString();
                    
                    if (objectsOnly) continue;
                    
                    bundledEnv.path = $"{subFolder}/{buildPrefabs.assetBundleName}";
                    bundledEnv.scenePath = $"{subFolder}/{buildScene.assetBundleName}";
                    
                    bundledEnv.hash = manifest.GetAssetBundleHash(buildPrefabs.assetBundleName).ToString();
                    bundledEnv.sceneHash = manifest.GetAssetBundleHash(buildScene.assetBundleName).ToString();
                }
            }

            return bundles;
        }

        public BundledEnv[] BuildBundles(Platform[] targets, bool objectsOnly = false)
        {
            return BuildBundles(CreateBuildInstructions(), targets, objectsOnly);
        }
    }
}