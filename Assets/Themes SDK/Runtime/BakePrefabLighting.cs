using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace OpenUp.Utils
{
    [ExecuteAlways]
    public class BakePrefabLighting : MonoBehaviour
    {
        [System.Serializable]
        public struct RendererInfo
        {
            public Renderer renderer;
            public int lightmapIndex;
            public Vector4 lightmapOffsetScale;
        }
        
        [System.Serializable]
        public struct LightInfo
        {
            public Light light;
            public int lightmapBaketype;
            public int mixedLightingMode;
        }

        [FormerlySerializedAs("m_RendererInfo")] 
        [SerializeField]
        public RendererInfo[] rendererInfos;
        
        [FormerlySerializedAs("m_Lightmaps")] 
        [SerializeField]
        public Texture2D[] lightmaps;
        
        [FormerlySerializedAs("m_LightmapsDir")] 
        [SerializeField]
        public Texture2D[] lightmapsDir;
        
        
        [FormerlySerializedAs("m_ShadowMasks")] 
        [SerializeField]
        public Texture2D[] shadowMasks;
        
        
        [FormerlySerializedAs("m_LightInfo")] 
        [SerializeField]
        public LightInfo[] lightInfos;
        
        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            if (rendererInfos == null || rendererInfos.Length == 0) return;

            LightmapData[] sceneLightmaps = LightmapSettings.lightmaps;
            int[] offsetsIndexes = new int[lightmaps.Length];
            int countTotal = sceneLightmaps.Length;
            List<LightmapData> combinedLightmaps = new List<LightmapData>();

            for (int i = 0; i < lightmaps.Length; i++)
            {
                offsetsIndexes[i] = countTotal;
                LightmapData newLightmapData = new LightmapData
                {
                    lightmapColor = lightmaps[i],
                    lightmapDir = lightmapsDir.Length == lightmaps.Length ? lightmapsDir[i] : default(Texture2D),
                    shadowMask = shadowMasks.Length == lightmaps.Length ? shadowMasks[i] : default(Texture2D),
                };

                combinedLightmaps.Add(newLightmapData);

                countTotal += 1;
            }

            LightmapData[] combinedLightmaps2 = new LightmapData[countTotal];

            sceneLightmaps.CopyTo(combinedLightmaps2, 0);
            combinedLightmaps.ToArray().CopyTo(combinedLightmaps2, sceneLightmaps.Length);

            bool directional = System.Array.TrueForAll(lightmapsDir, t => t != null);

            LightmapSettings.lightmapsMode = (lightmapsDir.Length == lightmaps.Length && directional) ? LightmapsMode.CombinedDirectional : LightmapsMode.NonDirectional;
            ApplyRendererInfo(rendererInfos, offsetsIndexes, lightInfos);
            LightmapSettings.lightmaps = combinedLightmaps2;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Init();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private static void ApplyRendererInfo(RendererInfo[] infos, int[] lightmapOffsetIndex, LightInfo[] lightsInfo)
        {
            foreach (RendererInfo info in infos)
            {
                info.renderer.lightmapIndex = lightmapOffsetIndex[info.lightmapIndex];
                info.renderer.lightmapScaleOffset = info.lightmapOffsetScale;

                // You have to release shaders.
                Material[] mats = info.renderer.sharedMaterials;
                foreach (Material mat in mats)
                {
                    if (mat != null && Shader.Find(mat.shader.name) != null)
                        mat.shader = Shader.Find(mat.shader.name);
                }
            }

            for (int i = 0; i < lightsInfo.Length; i++)
            {
                LightBakingOutput bakingOutput = new LightBakingOutput
                {
                    isBaked = true,
                    lightmapBakeType = (LightmapBakeType)lightsInfo[i].lightmapBaketype,
                    mixedLightingMode = (MixedLightingMode)lightsInfo[i].mixedLightingMode
                };

                lightsInfo[i].light.bakingOutput = bakingOutput;
            }
        }
    }
}