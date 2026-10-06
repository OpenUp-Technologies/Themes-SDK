using System;
using OpenUp.Environment.Features;
using OpenUp.Interpreter.Environment;
using UnityEngine;

namespace OpenUp.Environment
{
    [CreateAssetMenu(fileName = "EnvironmentOption", menuName = "OpenUp/Environments/EnvironmentOption", order = 0)]
    public class EnvironmentOption : ScriptableObject, IEnvironmentOption
    {
        public const string ROOT_OBJECT_NAME = "RootObject";
        public const string ROOT_OBJECT_NAME_AR = "RootObjectAR";
        public const string SKYBOX_NAME = "SkyBox";
        public const string SCENE_NAME = "RootScene";
        public const string STARTING_STATE_NAME = "StartingState";
        public const string THEMED_OBJECTS_NAME = "ThemedObjects";

        public string Name => environmentName;

        public string Id => databaseId;

        public string Version => "0.1";
        
        /// <summary>
        /// The name of the environment.
        /// </summary>
        public string environmentName;
   
        /// <summary>
        /// The id of the environment on the remote server.
        /// </summary>
        public string databaseId;
        public string orgId;

        /// <summary>
        /// The game object to load in.
        /// </summary>
        public LazyLoadReference<GameObject> rootObject;
        
        /// <summary>
        /// The game Object to load in for AR settings 
        /// </summary>
        public LazyLoadReference<GameObject> rootObjectAR;
        
        /// <summary>
        /// The skybox material to use for this environment.
        /// </summary>
        public LazyLoadReference<Material> skybox;

#if UNITY_EDITOR
        public UnityEditor.SceneAsset scene;
        
        public enum ModeSupport { Scene, RootObject, Both, None }
        
        public ModeSupport Supports  {
            get
            {
                if (scene && rootObject.isSet)
                    return ModeSupport.Both;
                
                if (scene)
                    return ModeSupport.Scene;

                if (rootObject.isSet)
                    return ModeSupport.RootObject;

                return ModeSupport.None;
            }
        }
        
        public enum Mode
        {
            [InspectorName("Root Object (Legacy)")]
            RootObject,
            [InspectorName("Scenes")]
            Scene,
            [InspectorName("Objects Only")]
            ObjectsOnly,
        }
        
        public Mode mode = Mode.Scene;
#endif
        /// <summary>
        /// Cannot use LazyLoadReference due to crash on device.
        /// </summary>
        public GameObject[] prefabs = Array.Empty<GameObject>();

        public string[] prefabPaths = Array.Empty<string>();
        public string[] PrefabPaths => prefabPaths;
        
        [SerializeField] public ThemedObject[] themedObjects = Array.Empty<ThemedObject>();
    }
}