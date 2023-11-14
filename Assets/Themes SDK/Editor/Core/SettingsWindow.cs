using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace OpenUp.Editor
{
    public class OpenUpEditorSettingsProvider : SettingsProvider
    {
        private const string MENU_NAME = "Project/OpenUp";
        private List<IMainFeatureSettings> features;

        private OpenUpEditorSettingsProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope) {}
                
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            return new OpenUpEditorSettingsProvider(MENU_NAME, SettingsScope.Project)
            {
                keywords = new[] {"Aryzon", "World"}
            };
        }
        
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            features = LoadFeatures();
            features.Sort((a, b) => b.Priority - a.Priority);

            foreach (IMainFeatureSettings feature in features) 
                feature.OnActivate();

            base.OnActivate(searchContext, rootElement);
        }

        public override void OnGUI(string searchContext)
        {
            foreach (IMainFeatureSettings feature in features)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(feature.Header, EditorStyles.boldLabel);

                feature.OnGUI();
            }
        }

        private List<IMainFeatureSettings> LoadFeatures()
        {
            return AppDomain.CurrentDomain
                            .GetAssemblies()
                            .SelectMany(asm => asm.GetTypes())
                            .Where(type => typeof(IMainFeatureSettings).IsAssignableFrom(type))
                            .Where(type => type.IsClass)
                            .Select(Activator.CreateInstance)
                            .Cast<IMainFeatureSettings>()
                            .ToList();
        }
    }
}
