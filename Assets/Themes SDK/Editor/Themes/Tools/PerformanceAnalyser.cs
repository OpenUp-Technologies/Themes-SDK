﻿using System;
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
        public record Renderer(GameObject Source, Mesh Mesh, int VertexCount);

        public const int RECOMMENDED_VERTEX_LIMIT = 500;
        public const int RECOMMENDED_COLLISION_FACES = 100;
        
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

            if (target.GetComponent<ConvertToSolution>())
                InteractableObjects++;

            if (target.GetComponent<Collider>())
                ColliderCount++;

            if (target.GetComponent<MeshCollider>())
            {
                MeshCollider mc = target.GetComponent<MeshCollider>();

                if (mc.convex)
                {
                    using Mesh.MeshDataArray data = Mesh.AcquireReadOnlyMeshData(mc.sharedMesh);
                    Mesh.MeshData meshData = data[0];

                    MeshColliderFaces += meshData.indexFormat switch
                    {
                        IndexFormat.UInt16 => meshData.GetIndexData<ushort>().Length / 3,
                        IndexFormat.UInt32 => meshData.GetIndexData<int>().Length / 3,
                    };
                }
                
            }

            return obj;
        }

        private Renderer GetRenderer(GameObject target)
        {
            if (target.GetComponent<MeshRenderer>() == null)
                return new Renderer(target, null, 0);
            
            MeshFilter filt = target.GetComponent<MeshFilter>();
            Mesh mesh = filt.sharedMesh ?? filt.mesh;
            Renderer renderer = new Renderer(target, mesh, mesh.vertexCount);
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
    }
}