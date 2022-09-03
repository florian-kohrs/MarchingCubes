using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class MarchingCubeMeshDisplayer
    {

        public Mesh mesh;

        public MeshFilter filter;

        public MeshRenderer renderer;

        public MeshCollider collider;

        public HasMarchingCube hasCube;

        public GameObject g;

        public bool HasCollider => collider != null;

        public bool HasChunk => hasCube != null && hasCube.chunk != null;

        public bool IsColliderActive => HasCollider && collider.sharedMesh != null;

        protected static int[] flatShadedMeshTriangleArray;

        static MarchingCubeMeshDisplayer()
        {
            int length = CompressedMarchingCubeChunk.MAX_TRIANGLES_PER_MESH + 1;
            flatShadedMeshTriangleArray = new int[length];
            for (int i = 0; i < length; i++)
            {
                flatShadedMeshTriangleArray[i] = i;
            }
        }

        protected MarchingCubeMeshDisplayer(ReducedMarchingCubesChunk chunk, GameObject g, Transform t) : this(g, g.AddComponent<MeshFilter>(), g.AddComponent<MeshRenderer>(), new Mesh(), g.AddComponent<MeshCollider>())
        {
            g.transform.SetParent(t, false);
            if (chunk is ReducedMarchingCubesChunk)
            {
                hasCube = g.AddComponent<HasMarchingCube>();
                hasCube.chunk = chunk;
            }
        }

        public void BakeMesh()
        {
            Physics.BakeMesh(collider.sharedMesh.GetInstanceID(), false);
        }


        protected MarchingCubeMeshDisplayer(GameObject g, Transform t) : this(g, g.AddComponent<MeshFilter>(), g.AddComponent<MeshRenderer>(), new Mesh())
        {
            g.transform.SetParent(t, false);
        }

        public MarchingCubeMeshDisplayer(ReducedMarchingCubesChunk chunk, Transform t) : this(chunk, new GameObject(/*$"{chunk.AnchorPos.x},{chunk.AnchorPos.y},{chunk.AnchorPos.z} "*/),t) { }

        public MarchingCubeMeshDisplayer(Transform t, bool useCollider) : this(new GameObject(),t) 
        { 
            if(useCollider)
            {
                GetCubeForwarder();
                GetCollider();
            }
        }

        public MarchingCubeMeshDisplayer(GameObject g, MeshFilter filter, MeshRenderer renderer, Mesh mesh, MeshCollider collider = null)
        {
            this.g = g;
            this.collider = collider;
            this.mesh = mesh;
            //this.mesh.Optimize();
            this.renderer = renderer;
            this.filter = filter;
            this.filter.mesh = mesh;
        }

        public void Reset()
        {
            if (hasCube != null)
            {
                hasCube.chunk = null;
                //hasCube.chunk = null;
            }
            if (collider != null)
            {
                collider.sharedMesh = null;
            }
            mesh.Clear();
        }

        protected MeshCollider GetCollider()
        {
            if(collider == null)
            {
                collider = g.AddComponent<MeshCollider>();
            }
            return collider;
        }

        protected HasMarchingCube GetCubeForwarder()
        {
            if (hasCube == null)
            {
                hasCube = g.AddComponent<HasMarchingCube>();
            }
            return hasCube;
        }

        public void SetInteractableChunk(ReducedMarchingCubesChunk chunk)
        {
            if (chunk != null)
            {
                GetCubeForwarder().chunk = chunk;
            }
        }




        public void ApplyMesh(Color32[] colorData, Vector3[] vertices, Material mat, bool useCollider = true)
        {
            mesh.vertices = vertices;
            mesh.colors32 = colorData;
            int[] meshTriangles = new int[colorData.Length];
            Array.Copy(flatShadedMeshTriangleArray, meshTriangles, meshTriangles.Length);
            mesh.triangles = meshTriangles;
            renderer.material = mat;
            mesh.RecalculateNormals();
            if (useCollider)
            {
                GetCollider().sharedMesh = mesh;
            }
        }

    }
}