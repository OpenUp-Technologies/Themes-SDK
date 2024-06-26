using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Scripts in asset bundles are saved under their full name and assembly name,
// don't rename these scripts as all themes would need to be rebuilt.
// see: https://forum.unity.com/threads/asset-bundle-cannot-find-scripts-from-assembly-definition-files.531804/#post-3521699


namespace OpenUp.Interpreter.Utils
{
    [ExecuteInEditMode]
    public class PlayableObject : MonoBehaviour
    {
        /// <summary>
        /// The path to the model source.
        /// </summary>
        [SerializeField] public string source;
        
        /// <summary>
        /// Turns of physics for the object. 
        /// </summary>
        [Header("Physics")]
        [SerializeField] 
        public bool noPhysics;
        
        /// <summary>
        /// The weight of the object, if physics is turned on.
        /// </summary>
        [SerializeField] public float weight = 1;
        [SerializeField] public Transform centerOfMassObject = null;

        /// <summary>
        /// Drag make the object fall slower and appear floaty.
        /// </summary>
        [SerializeField] public float drag = 1;
        
#if UNITY_EDITOR
        private void OnEnable()
        {
            if (source == null)
                SetPathToExpected();
        }

        public string GetExpectedPath()
        {
            GameObject prefab = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);

            if (prefab != gameObject)
                return null;
            
            GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefab);
            
            return AssetDatabase.GetAssetPath(prefabAsset);
        }

        public void SetPathToExpected()
        {
            string expected = GetExpectedPath();

            if (System.String.IsNullOrWhiteSpace(expected))
            {
                Debug.LogError($"Object {gameObject.name} is not an instance of a prefab. In-game objects have to be prefabs.");
                return;
            }

            source = expected;
            
            EditorUtility.SetDirty(gameObject);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}