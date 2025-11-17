using System;
using UnityEngine;

namespace OpenUp.Environment.Features
{
    /// <summary>
    /// A themed object is an object that fits the theme that players can add to your world.
    /// </summary>
    [Serializable]
    public class ThemedObject
    {
        public GameObject GameObject;
        public string DisplayName;
        public string Thumbnail; 
        public string RelativePath;
    }
    
    public class ThemedObjectsList : ScriptableObject
    {
        [SerializeField] public ThemedObject[] ThemedObjects;
    }
}