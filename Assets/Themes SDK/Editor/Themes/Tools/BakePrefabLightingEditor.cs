using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static OpenUp.Utils.BakePrefabLighting;

namespace OpenUp.Utils
{
    [ExecuteAlways]     
    [CustomEditor(typeof(BakePrefabLighting))]
    public class BakePrefabLightingEditor: UnityEditor.Editor
    {
        [MenuItem("Assets/Bake Prefab Lightmaps")]
        public static void GenerateLightmapInfo()
        {
            if (Lightmapping.giWorkflowMode != Lightmapping.GIWorkflowMode.OnDemand)
            {
                Debug.LogError("ExtractLightmapData requires that you have baked you lightmaps and Auto mode is disabled.");
                return;
            }
            
            Lightmapping.Bake();

            BakePrefabLighting[] prefabs = FindObjectsOfType<BakePrefabLighting>();

            foreach (BakePrefabLighting instance in prefabs)
                SaveLighting(instance);
        }

        private static void SaveLighting(BakePrefabLighting instance)
        {
            GameObject gameObject = instance.gameObject;
            List<RendererInfo> rendererInfos = new List<RendererInfo>();
            List<Texture2D> lightmaps = new List<Texture2D>();
            List<Texture2D> lightmapsDir = new List<Texture2D>();
            List<Texture2D> shadowMasks = new List<Texture2D>();
            List<LightInfo> lightsInfos = new List<LightInfo>();

            GenerateLightmapInfo(gameObject, rendererInfos, lightmaps, lightmapsDir, shadowMasks, lightsInfos);

            instance.rendererInfos = rendererInfos.ToArray();
            instance.lightmaps = lightmaps.ToArray();
            instance.lightmapsDir = lightmapsDir.ToArray();
            instance.lightInfos = lightsInfos.ToArray();
            instance.shadowMasks = shadowMasks.ToArray();
            
            GameObject targetPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance.gameObject);
            if (targetPrefab != null)
            {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(instance.gameObject);
                if (root != null)
                {
                    GameObject rootPrefab = PrefabUtility.GetCorrespondingObjectFromSource(instance.gameObject);
                    string rootPath = AssetDatabase.GetAssetPath(rootPrefab);

                    PrefabUtility.UnpackPrefabInstanceAndReturnNewOutermostRoots(root, PrefabUnpackMode.OutermostRoot);

                    try
                    {
                        PrefabUtility.ApplyPrefabInstance(instance.gameObject, InteractionMode.AutomatedAction);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning($"Failed to apply changes to {instance.gameObject}\n{exception}");
                    }
                    finally
                    {
                        PrefabUtility.SaveAsPrefabAssetAndConnect(root, rootPath, InteractionMode.AutomatedAction);
                    }
                }
                else
                {
                    PrefabUtility.ApplyPrefabInstance(instance.gameObject, InteractionMode.AutomatedAction);
                }
            }
        }

        private static void GenerateLightmapInfo(GameObject root, List<RendererInfo> rendererInfos, List<Texture2D> lightmaps, List<Texture2D> lightmapsDir, List<Texture2D> shadowMasks, List<LightInfo> lightsInfo)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer.lightmapIndex == -1) continue;
                if (renderer.lightmapScaleOffset == Vector4.zero) continue;
                if (renderer.gameObject.layer != 22) continue;

                Texture2D lightmap    = LightmapSettings.lightmaps[renderer.lightmapIndex].lightmapColor;
                Texture2D lightmapDir = LightmapSettings.lightmaps[renderer.lightmapIndex].lightmapDir;
                Texture2D shadowMask  = LightmapSettings.lightmaps[renderer.lightmapIndex].shadowMask;

                RendererInfo info = new RendererInfo
                {
                    renderer            = renderer,
                    lightmapOffsetScale = renderer.lightmapScaleOffset,
                    lightmapIndex       = lightmaps.IndexOf(lightmap)
                };

                if (info.lightmapIndex == -1)
                {
                    info.lightmapIndex = lightmaps.Count;
                    lightmaps.Add(lightmap);
                    lightmapsDir.Add(lightmapDir);
                    shadowMasks.Add(shadowMask);
                }

                rendererInfos.Add(info);
            }

            var lights = root.GetComponentsInChildren<Light>(true);

            foreach (Light l in lights)
            {
                LightInfo lightInfo = new LightInfo
                {
                    light             = l,
                    lightmapBaketype  = (int) l.lightmapBakeType,
                    mixedLightingMode = (int) Lightmapping.lightingSettings.mixedBakeMode
                };

                lightsInfo.Add(lightInfo);

            }
        }
    }
}
