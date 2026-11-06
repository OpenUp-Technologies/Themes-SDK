using OpenUp.Editor.EnvironmentsSdk;
using UnityEditor;
using UnityEngine;

namespace Themes_SDK.Editor.Themes.UI.Custom_Inspectors
{
    [CustomEditor(typeof(ThumbnailCam))]
    public class ThumbnailCamInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (GUILayout.Button("Generate Image"))
            {
                ThumbnailGenerator.StoreImage(target as ThumbnailCam);
            }
        }
        
    }
}