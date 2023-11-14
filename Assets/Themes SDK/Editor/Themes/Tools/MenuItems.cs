using System;
using OpenUp.Interpreter.Utils;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Editor.EnvironmentsSdk
{
    internal static class MenuItems
    {
        /// <remarks>
        /// Note that this gets fired for each object individually if you have multiple selected.
        /// </remarks>
        /// <param name="cmd">The individual object this command is being executed on.</param>
        [MenuItem("GameObject/OpenUp/Make In-game Objects")]
        public static void MakeConvertable(MenuCommand cmd)
        {
            if (cmd.context is GameObject gameObject) AddConvert(gameObject);
        }

        private static void AddConvert(GameObject target)
        {
            GameObject prefab = PrefabUtility.GetNearestPrefabInstanceRoot(target);

            if (prefab != target)
            {
                Debug.LogError($"Object {target.name} is not an instance of a prefab. In-game objects have to be prefabs.");
                return;
            }

            if (prefab.GetComponent<ConvertToSolution>())
            {
                Debug.LogWarning($"Object {target.name} will already be converted, skipping.");
                return;
            }
            
            GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
            
            string path = AssetDatabase.GetAssetPath(prefabAsset);

            // Makes sure the undo command can clean it up if unwanted
            ConvertToSolution convert = Undo.AddComponent<ConvertToSolution>(target);

            convert.source = path;
        }

        [MenuItem("OpenUp/Themes/Create New Theme")]
        public static void CreateNewEnvironment()
        {
            EditorWindow.GetWindow<CreateEnvUI>().Show();
        }
        
        [MenuItem("OpenUp/Themes/Manage Custom Themes &#e")]
        public static void ManageEnvironments()
        {
            SettingsService.OpenProjectSettings("Project/OpenUp/Environments Settings");
        }
    }
}