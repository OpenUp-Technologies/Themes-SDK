using UnityEditor;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public static class PerformanceUI
    {
        public static void DrawWarnings(PerformanceAnalysis analysis)
        {
            if (analysis == null)
            {
                EditorGUILayout.HelpBox("Could not run analysis, the most likely cause of this is a missing mesh collider", MessageType.Warning);
            }
            
            if (analysis.TotalVertices > PerformanceAnalysis.RECOMMENDED_VERTEX_LIMIT)
                EditorGUILayout.HelpBox(
                    "Root map exceeds the recommended maximum vertex count. Players might notice degraded performance when using this theme.\n" +
                    $"Current: {analysis.TotalVertices}\nRecommended: {PerformanceAnalysis.RECOMMENDED_VERTEX_LIMIT}", 
                    MessageType.Warning);
            
            if (analysis.MeshColliderFaces > PerformanceAnalysis.RECOMMENDED_COLLISION_FACES)
                EditorGUILayout.HelpBox(
                    "Root map contains highly complex collision meshes. This can make the physics in the world very slow and choppy, particularly on mobile and VR devices.\n" +
                    $"Current: {analysis.MeshColliderFaces}\nRecommended: {PerformanceAnalysis.RECOMMENDED_COLLISION_FACES}", 
                    MessageType.Warning);
        }
    }
}