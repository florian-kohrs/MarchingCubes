using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class BaseMeshDisplayer
    {

        public Mesh mesh;

        public MeshFilter filter;

        public MeshRenderer renderer;

        public MeshCollider collider;

        public HasMarchingCube hasCube;

        public GameObject g;

        public bool HasCollider => collider != null;

        public bool IsColliderActive => HasCollider && collider.sharedMesh != null;

        protected BaseMeshDisplayer(IMarchingCubeInteractableChunk chunk, GameObject g, Transform t) : this(g, g.AddComponent<MeshFilter>(), g.AddComponent<MeshRenderer>(), new Mesh(), g.AddComponent<MeshCollider>())
        {
            g.transform.SetParent(t,false);
            if (chunk is IMarchingCubeInteractableChunk interactable)
            {
                hasCube = g.AddComponent<HasMarchingCube>();
                hasCube.chunk = chunk;
            }
        }

        protected BaseMeshDisplayer(GameObject g, Transform t) : this(g, g.AddComponent<MeshFilter>(), g.AddComponent<MeshRenderer>(), new Mesh())
        {
            g.transform.SetParent(t, false);
        }

        public BaseMeshDisplayer(IMarchingCubeInteractableChunk chunk, Transform t) : this(chunk, new GameObject(),t) { }

        public BaseMeshDisplayer(Transform t) : this(new GameObject(),t) { }

        public BaseMeshDisplayer(GameObject g, MeshFilter filter, MeshRenderer renderer, Mesh mesh, MeshCollider collider = null)
        {
            this.g = g;
            this.collider = collider;
            this.mesh = mesh;
            this.renderer = renderer;
            this.filter = filter;
            this.filter.mesh = mesh;
        }

        public void Reset()
        {
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

        public void SetInteractableChunk(IMarchingCubeInteractableChunk chunk)
        {
            if (chunk != null)
            {
                GetCubeForwarder().chunk = chunk;
            }
        }

        public void ApplyMesh(Color[] colorData, Vector3[] vertices, int[] meshTriangles, Material mat, bool useCollider = true)
        {
            mesh.vertices = vertices;
            mesh.colors = colorData;
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