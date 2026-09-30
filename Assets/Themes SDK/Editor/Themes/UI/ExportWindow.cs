using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUp.Environment;
using OpenUp.Interpreter.Environment;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public class ExportWindow : EditorWindow
    {
        private readonly HashSet<Platform> chosenPlatforms = new HashSet<Platform>();
        
        private PerformanceAnalysis analysis;
        private EnvironmentOption localAsset;
        private string storePath;

        public void SetAsset(EnvironmentOption option)
        {
            localAsset = option;
            analysis = PerformanceAnalysis.Analyse(localAsset.rootObject.asset);
        }

        private void OnGUI()
        {
            titleContent = new GUIContent(localAsset.Name);
            
            PerformanceUI.DrawWarnings(analysis);
            
            RenderPlatformSelection();
            
            if (GUILayout.Button("Save")) RunSave();
        }

        private void RenderPlatformSelection()
        {
            string plats = chosenPlatforms.Aggregate("", (s, p) => String.IsNullOrEmpty(s) ? p.ToString() : $"{s}, {p}");

            if (GUILayout.Button(String.IsNullOrWhiteSpace(plats) ? "<Choose Build Targets>" : plats))
            {
                GenericMenu menu = new GenericMenu();
                
                foreach (Platform platform in Enum.GetValues(typeof(Platform)))
                {
                    void ChosePlat(object plat)
                    {
                        if (!chosenPlatforms.Add(platform)) chosenPlatforms.Remove(platform);
                    }

                    menu.AddItem(new GUIContent(platform.ToString()), chosenPlatforms.Contains(platform), ChosePlat, new object());
                }
                
                menu.ShowAsContext();
            }
        }

        private void RunSave()
        {
            storePath = EditorUtility.SaveFilePanel(
                "Choose where to save your theme", 
                Application.dataPath, 
                localAsset.Name, "thm");
            
            if (!String.IsNullOrWhiteSpace(storePath))
            {
                Exporter exporter = new Exporter(localAsset);
                exporter.Run(new Exporter.Settings(storePath, chosenPlatforms));
                
                #if UNITY_EDITOR_WIN
                FileInfo inf = new FileInfo(storePath);
                System.Diagnostics.Process.Start("explorer.exe","/select,"+inf.DirectoryName);
                #endif
                
                Close();
            }
            else
            {
                Debug.LogError("No path selected");
            }
        }
    }
}