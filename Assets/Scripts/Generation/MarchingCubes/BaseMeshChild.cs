using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class BaseMeshChild
    {

        public Mesh mesh;

        public MeshFilter filter;

        public MeshRenderer renderer;

        public MeshCollider collider;

        protected BaseMeshChild(IMarchingCubeInteractableChunk chunk, GameObject g, Transform t) : this(g.AddComponent<MeshFilter>(), g.AddComponent<MeshRenderer>(), g.AddComponent<MeshCollider>(), new Mesh())
        {
            g.transform.SetParent(t,false);
            g.AddComponent<HasMarchingCube>().chunk = chunk;
        }

        public BaseMeshChild(IMarchingCubeInteractableChunk chunk, Transform t) : this(chunk, new GameObject(),t)
        {
        }

        public void Reset()
        {
            IsAppliedMesh = false;
            mesh.Clear();
        }

        public void ApplyMesh(Color[] colorData, Vector3[] vertices, int[] meshTriangles, Material mat, bool useCollider)
        {
            IsAppliedMesh = true;
            mesh.vertices = vertices;
            mesh.colors = colorData;
            mesh.triangles = meshTriangles;
            renderer.material = mat;
            mesh.RecalculateNormals();
            if (useCollider)
            {
                collider.sharedMesh = mesh;
            }
        }

        public bool IsAppliedMesh { get; private set; }

        public BaseMeshChild(MeshFilter filter, MeshRenderer renderer, MeshCollider collider, Mesh mesh)
        {
            this.collider = collider;
            this.mesh = mesh;
            this.renderer = renderer;
            this.filter = filter;

            this.filter.mesh = mesh;
        }

    }
}