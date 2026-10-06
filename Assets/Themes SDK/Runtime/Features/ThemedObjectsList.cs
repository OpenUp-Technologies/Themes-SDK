using System;
using Newtonsoft.Json;
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
        public string RelativePath;
        public Texture2D Thumbnail;
    }
    
    public class ThemedObjectsList : ScriptableObject
    {
        [SerializeField] public ThemedObject[] ThemedObjects;
        public string PrintList() => string.Join("\n", Array.ConvertAll(ThemedObjects, obj => 
            $"Object {obj.DisplayName}:\n\tPath: {obj.RelativePath}\n\tThumbnail:{obj.Thumbnail}\n\tGameObject:{obj.GameObject}"));
    }
}