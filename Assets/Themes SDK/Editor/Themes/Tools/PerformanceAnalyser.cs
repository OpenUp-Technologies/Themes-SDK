using System;
using System.Collections.Generic;
using System.Linq;
using OpenUp.Interpreter.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public class PerformanceAnalysis
    {
        public record HierarchyObject(Renderer Self, int[] Children, int Total);
        public record Renderer(GameObject Source, Mesh Mesh, int VertexCount, float Density);

        public const int RECOMMENDED_VERTEX_LIMIT = 500_000;
        public const int RECOMMENDED_COLLISION_FACES = 10_000;
        
        private readonly Dictionary<int, HierarchyObject> objects = new Dictionary<int, HierarchyObject>();
        private readonly Dictionary<int, Renderer> renderers = new Dictionary<int, Renderer>();
        public IReadOnlyCollection<Renderer> Renderers => renderers.Values;
        public IReadOnlyDictionary<int, HierarchyObject> Objects => objects;
        public int  TotalVertices { get; private set; }
        public int InteractableObjects { get; private set; }
        public int ColliderCount { get; private set; }
        public int MeshColliderFaces { get; private set; }

        public string ImpactOf(int targetId, bool cascade = false)
        {
            int value = VerticesOf(targetId, cascade);
            
            if (value == 0) 
                return String.Empty;
            else 
                return $"{100*value / (float)TotalVertices:F1}%";
        }

        public int VerticesOf(int targetId, bool cascade = false)
        {
            if (!objects.TryGetValue(targetId, out HierarchyObject renderer))
                return 0;
            
            return cascade ? renderer.Total : renderer.Self.VertexCount;
        }
        
        public float DensityOf(int targetId, bool cascade = false)
        {
            if (!objects.TryGetValue(targetId, out HierarchyObject renderer))
                return 0;
            
            return cascade ? throw new NotImplementedException() : renderer.Self.Density;
        }

        private HierarchyObject Add(GameObject target)
        {
            Dictionary<int, HierarchyObject> kids = new Dictionary<int, HierarchyObject>(target.transform.childCount);

            foreach (Transform child in target.transform)
                kids.Add(child.gameObject.GetInstanceID(), Add(child.gameObject));

            Renderer self = GetRenderer(target);
            HierarchyObject obj = new HierarchyObject(
                self,
                kids.Keys.ToArray(),
                kids.Values.Sum(ho => ho.Total) + self.VertexCount
            );
            
            objects.Add(target.GetInstanceID(), obj);

            if (target.GetComponent<PlayableObject>())
                InteractableObjects++;

            if (target.GetComponent<Collider>())
                ColliderCount++;

            if (target.GetComponent<MeshCollider>())
            {
                MeshCollider mc = target.GetComponent<MeshCollider>();

                using Mesh.MeshDataArray data = Mesh.AcquireReadOnlyMeshData(mc.sharedMesh);
                Mesh.MeshData meshData = data[0];

                MeshColliderFaces += meshData.indexFormat switch
                {
                    IndexFormat.UInt16 => meshData.GetIndexData<ushort>().Length / 3,
                    IndexFormat.UInt32 => meshData.GetIndexData<int>().Length / 3,
                };
            }

            return obj;
        }

        private Renderer GetRenderer(GameObject target)
        {
            if (target.GetComponent<UnityEngine.Renderer>() == null)
                return new Renderer(target, null, 0, 0);
            
            MeshFilter filt = target.GetComponent<MeshFilter>();
            SkinnedMeshRenderer smr = target.GetComponent<SkinnedMeshRenderer>();
            
            Mesh mesh = filt != null ? (filt.sharedMesh ?? filt.mesh)
                                     : smr.sharedMesh;

            float density = mesh.vertexCount / VolumeOf(mesh.bounds);
            density /= target.transform.lossyScale.x;
            density /= target.transform.lossyScale.y;
            density /= target.transform.lossyScale.z;
            
            Renderer renderer = new Renderer(target, mesh, mesh.vertexCount, density);
            renderers.Add(target.GetInstanceID(), renderer);
            TotalVertices += renderer.VertexCount;

            return renderer;
        }
        
        public static PerformanceAnalysis Analyse(params GameObject[] targets)
        {
            PerformanceAnalysis analysis = new PerformanceAnalysis();

            foreach (GameObject target in targets)
                analysis.Add(target);

            return analysis;
        }

        private float VolumeOf(Bounds bounds)
        {
            float volume = 1;

            for (int i = 0; i < 3; i++)
                if (bounds.size[i] > 0.003f) 
                    volume *= bounds.size[i];

            return volume;
        }
    }
}