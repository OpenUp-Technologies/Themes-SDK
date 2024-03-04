using System;
using System.Collections.Generic;
using System.Linq;
using OpenUp.Editor.UI;
using OpenUp.Interpreter.Environment;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public class EnvironmentsSettingsWindow : SettingsProvider
    {
        public const string APP_VERSION = "2023.0.0";
        public const string MENU_NAME  = "Project/OpenUp/Custom Themes";

        private EnvironmentsEditor core;
        
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            EnvironmentsSettingsWindow provider = new EnvironmentsSettingsWindow(MENU_NAME, SettingsScope.Project)
                                            {
                                                keywords = new[] { "Themes" }
                                            };
            return provider;
        }

        private EnvironmentsSettingsWindow(string path, SettingsScope scope = SettingsScope.User) : base(path, scope) {}

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            
            core = new EnvironmentsEditor();
            
            core.FetchRemotes();
            core.FindLocalOptions();
        }

        public override void OnGUI(string searchContext)
        {
            if (DevProfileUI.RenderUnavailable())
            {
                EditorGUILayout.LabelField("You are not logged in as a developer. You must be logged in to access the themes API.", EditorStyles.helpBox);

                return;
            }
            
            RenderAddButton();
            
            core.GuiTick();
            
            new RemoteOptions(core).Render();
        }

        private void RenderAddButton()
        {
            if (GUILayout.Button("Create new Theme")) 
                MenuItems.CreateNewEnvironment();
        }
    }
}