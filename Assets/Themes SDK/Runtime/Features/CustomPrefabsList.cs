using System;
using UnityEngine;

namespace OpenUp.Environment.Features
{
    [Serializable]
    public class CustomPrefab
    {
        public GameObject GameObject;
        public string DisplayName;
        public string ThumbnailImage;
        [HideInInspector] public string Path;
    }
    
    public class CustomPrefabsList : ScriptableObject
    {
        [SerializeField] public CustomPrefab[] Prefabs;
    }
}