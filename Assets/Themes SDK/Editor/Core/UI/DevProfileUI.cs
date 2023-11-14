using OpenUp.Networking.Editor;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Editor.UI
{
    public class DevProfileUI : IMainFeatureSettings
    {
        public string Header => "Developer Profile";
        public int Priority => 10;
        public void OnActivate() { }
        
        public void OnGUI()
        {
            if (RenderUnavailable()) 
                return;
            
            DeveloperProfile chosenProfile = EditorGUILayout.ObjectField(
                "Profile",
                DeveloperProfile.Instance,
                typeof(DeveloperProfile),
                false
            ) as DeveloperProfile;

            if (chosenProfile != DeveloperProfile.Instance)
            {
                if (!chosenProfile) 
                    DeveloperProfile.ClearActive();
                else
                    chosenProfile.SetActive();
                
                return;
            }
            else if (chosenProfile == null) 
                return;
            
            StoreProfileField(ref chosenProfile.username, EditorGUILayout.DelayedTextField("Username", chosenProfile.username));
            StoreProfileField(ref chosenProfile.server, EditorGUILayout.DelayedTextField("Server", chosenProfile.server));

            if (!chosenProfile.isLoggedIn && GUILayout.Button("Log In"))
            {
                LoginWindow.GetWindow(chosenProfile);
            }
            else if (chosenProfile.isLoggedIn && GUILayout.Button("Log Out"))
            {
                chosenProfile.Logout();
            }
        }
        
        public static bool RenderUnavailable()
        {
            if (DeveloperProfile.Instance != null &&
                DeveloperProfile.Instance.isLoggedIn) return false;

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = DeveloperProfile.Instance != null;
            
            bool buttonPressed = GUILayout.Button("Log in as:", GUILayout.Width(150));

            GUI.enabled = true;
            
            DeveloperProfile chosenProfile = EditorGUILayout.ObjectField(
                DeveloperProfile.Instance,
                typeof(DeveloperProfile),
                false
            ) as DeveloperProfile;

            if (chosenProfile != DeveloperProfile.Instance)
            {
                chosenProfile!.SetActive();
            }
            
            if (buttonPressed)
            {
                LoginWindow.GetWindow(chosenProfile);
            }
            
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Create new profile", GUILayout.Width(150)))
            {
                EditorWindow.GetWindow<CreateNewDevProfile>();
            }
            
            return true;
        }
        
        private void StoreProfileField(ref string field, string newValue)
        {
            if (field != newValue)
            {
                field = newValue;
                EditorUtility.SetDirty(DeveloperProfile.Instance);
                AssetDatabase.SaveAssets();
            }
        }
    }
}