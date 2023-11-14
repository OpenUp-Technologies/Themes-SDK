using System;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Networking.Editor
{
    public class LoginWindow : EditorWindow
    {
        private static DeveloperProfile profile;
        private string pass;

        private bool triedLogin = false;
        
        public void OnGUI()
        {
            profile = EditorGUILayout.ObjectField("Profile", profile, typeof(DeveloperProfile), false) as DeveloperProfile;
            pass = EditorGUILayout.PasswordField("Password", pass);

            GUI.enabled = !profile?.isLoggingIn ?? false;
            
            if (GUILayout.Button("Log in"))
            {
                triedLogin = true;
                profile!.Authorize(pass);
            }

            GUI.enabled = true;
            
            if (triedLogin && (profile?.isLoggedIn ?? false)) Close();
        }

        private void OnEnable()
        {
            pass = "";
            triedLogin = false;
        }

        public static void GetWindow(DeveloperProfile profile)
        {
            LoginWindow.profile = profile;
            GetWindow<LoginWindow>().Show();
        }
    }
}