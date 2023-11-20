using System;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public static class HierarchyAddOn
    {
        private enum Mode
        {
            CountIndividual,
            CountTotal,
            PercentIndividual,
            PercentTotal
        }
        
        private static bool hasAddedListener = false;
        private static bool isActive;
        private static PerformanceAnalysis currentAnalysis;
        private static Mode mode;

        private static readonly Color LowImpact = new Color(0.3f, 1, 0.5f, 0.1f);
        private static readonly Color HighImpact = new Color(0.6f, 0.1f, 0.1f, 0.6f);
        private const int HighVertexCount = 3000;
        private static void AddHierarchyListener()
        {
            if (hasAddedListener) 
                return;
            
            
            hasAddedListener = true;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        public static void Display(PerformanceAnalysis analysis)
        {
            AddHierarchyListener();
            currentAnalysis = analysis; 
            isActive = true;
        }

        public static void Hide() => isActive = false;

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            if (!isActive)
                return;
            
            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            // Scenes are in the hierarchy but we don't want to add them here.
            if (go == null)
                return;

            DrawText(instanceID, selectionRect);
            DrawOverlay(instanceID, selectionRect);
        }

        private static void DrawOverlay(int instanceID, Rect selectionRect)
        {
            if (mode is Mode.PercentIndividual or Mode.PercentTotal)
                return;

            int count = mode switch
            {
                Mode.CountIndividual => currentAnalysis.VerticesOf(instanceID),
                Mode.CountTotal => currentAnalysis.VerticesOf(instanceID, true),
                _ => throw new ArgumentException()
            };

            float lerpValue = count / (float)HighVertexCount;
            lerpValue = lerpValue > 0 ? Mathf.Sqrt(lerpValue) : lerpValue;

            Color color = Color.Lerp(LowImpact, HighImpact, lerpValue);
            
            Rect rect = new Rect(selectionRect);
            rect.min = rect.max + Vector2.left * 50 + Vector2.down * 15;
            
            EditorGUI.DrawRect(rect, color);
        }
        
        private static void DrawText(int instanceID, Rect selectionRect)
        {
            Rect rect = new Rect(selectionRect);
            rect.min = rect.max + Vector2.left * 50 + Vector2.down * 15;

            string HideZeros(int i) => i > 0 ? i.ToString() : String.Empty;

            string value = mode switch
            {
                Mode.PercentTotal => currentAnalysis.ImpactOf(instanceID, true),
                Mode.PercentIndividual => currentAnalysis.ImpactOf(instanceID, false),
                Mode.CountTotal => HideZeros(currentAnalysis.VerticesOf(instanceID, true)),
                Mode.CountIndividual => HideZeros(currentAnalysis.VerticesOf(instanceID, false)),
                _ => throw new ArgumentException()
            };
            
            EditorGUI.LabelField(rect, value);
        } 
        
        private static void ShowAnalysis(MenuCommand cmd, Mode _mode)
        {
            mode = _mode;
            
            if (cmd.context is not GameObject gameObject)
                return;
            
            if (Selection.objects.Length < 2)
                ShowFor(gameObject);
            else if (Selection.gameObjects[0] == gameObject)
                ShowFor(Selection.gameObjects);
        }

        private static void ShowFor(params GameObject[] targets)
        {
            PerformanceAnalysis analysis = PerformanceAnalysis.Analyse(targets);
            Display(analysis);
        }

        [MenuItem("GameObject/Analysis/Vertices (Individual)")]
        private static void ShowAnalysisVertsIndividual(MenuCommand cmd) 
            => ShowAnalysis(cmd, Mode.CountIndividual);
        
        [MenuItem("GameObject/Analysis/Vertices (Full)")]
        private static void ShowAnalysisVertsFull(MenuCommand cmd) 
            => ShowAnalysis(cmd, Mode.CountTotal);
        
        [MenuItem("GameObject/Analysis/Percentages (Individual)")]
        private static void ShowAnalysisPercentsIndividual(MenuCommand cmd) 
            => ShowAnalysis(cmd, Mode.PercentIndividual);
        
        [MenuItem("GameObject/Analysis/Percentages (Full)")]
        private static void ShowAnalysisPercentsFull(MenuCommand cmd) 
            => ShowAnalysis(cmd, Mode.PercentTotal);
    }
}