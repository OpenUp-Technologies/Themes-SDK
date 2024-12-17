using System;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public class PerformanceWindow : EditorWindow
    {
        private GameObject[] targets;
        private PerformanceAnalysis analysis;

        private void RunAnalysis()
        {
            analysis = PerformanceAnalysis.Analyse(targets);
            HierarchyAddOn.Display(analysis);
        }

        private void OnDisable()
        {
            HierarchyAddOn.Hide();
        }

        private void OnGUI()
        {
            if (analysis == null)
            {
                EditorGUILayout.HelpBox("Could not run performance analysis", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"Analysed {analysis.Renderers.Count} objects", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Total vertex count:", analysis.TotalVertices.ToString());
            }
        }

        /// <remarks>
        /// Note that this gets fired for each object individually if you have multiple selected.
        /// </remarks>
        /// <param name="cmd">The individual object this command is being executed on.</param>
        // TODO: This menu has been disabled for now. The tool is in a decent enough state for now.
        // [MenuItem("GameObject/OpenUp/Analyse Performance")]
        public static void AnalysePerformance(MenuCommand cmd)
        {
            if (cmd.context is not GameObject gameObject)
                return;
            
            if (Selection.objects.Length < 2)
                ShowFor(gameObject);
            else if (Selection.gameObjects[0] == gameObject)
                ShowFor(Selection.gameObjects);
        }

        private static void ShowFor(params GameObject[] targets)
        {
            PerformanceWindow window = GetWindow<PerformanceWindow>();
            window.targets = targets;
            window.RunAnalysis();
            
            window.Show();
        }
    }
}