using UnityEditor;
using UnityEngine;

namespace OpenUp.Networking.Editor
{
    public class CreateNewDevProfile : EditorWindow
    {
        private string profName = "";
        private string username = "";
        private string password = "";

        private DeveloperProfile profile;

        public void OnGUI()
        {
            profName = EditorGUILayout.TextField("Profile name", profName);
            username = EditorGUILayout.TextField("Username", username);
            password = EditorGUILayout.PasswordField("Password", password);

            if (profile == null)
            {
                if (GUILayout.Button("Create"))
                {
                    profile = ScriptableObject.CreateInstance<DeveloperProfile>();
                    profile.username = username;
                    profile.server = "https://prod-server.openuptech.com/";
                    
                    profile.Authorize(password);
                }
            }
            else if (profile.isLoggingIn)
            {
                GUI.enabled = false;
                GUILayout.Button("Authenticating...");
                GUI.enabled = true;
            }
            else if (profile.isLoggedIn)
            {
                string loc = EditorUtility.SaveFilePanelInProject(
                    "Save Developer Profile", 
                    profName, 
                    "asset",
                    "Select where to save the profile");
                
                AssetDatabase.CreateAsset(profile, loc);
                AssetDatabase.SaveAssets();
                
                profile.SetActive();
                
                Close();
            }
            else
            {
                profile = null;
            }
        }
    }
}