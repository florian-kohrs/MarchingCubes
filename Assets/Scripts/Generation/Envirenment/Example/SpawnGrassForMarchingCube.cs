using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshGPUInstanciation
{
    public class SpawnGrassForMarchingCube : MonoBehaviour
    {

        protected static string NAME_OF_TRIANGLE_BUFFER = "meshTriangles";
        protected static string NAME_OF_OFFSET = "offset";
        protected static string NAME_OF_BOUNDS = "boundsHeight";
        protected static string NAME_OF_TRIANGLE_LENGTH = "length";
        protected static string NAME_OF_GRASS_BUFFER = "grassPositions";

        [System.NonSerialized]
        protected ComputeBuffer grassProperties;

        public ComputeShader grassShader;
        public Material mat;
        public Mesh grassMesh;


        public const int GRASS_PER_COMPUTE = 32 * 32 * 5 * 4;

        public void ComputeGrassFor(Bounds bounds, int numTris, ComputeBuffer triangleData)
        {
            int numThreadPerAxis = Mathf.Max(1,Mathf.CeilToInt(numTris / 32f));
            grassProperties = new ComputeBuffer(GRASS_PER_COMPUTE, MeshInstancedProperties.Size(), ComputeBufferType.Append);
            grassProperties.SetCounterValue(0);
            Material mat = new Material(this.mat);
            Vector3 offset = bounds.center;

            grassShader.SetInt(NAME_OF_TRIANGLE_LENGTH, numTris);
            grassShader.SetVector(NAME_OF_OFFSET, offset);
            grassShader.SetFloat(NAME_OF_BOUNDS, grassMesh.bounds.extents.y);
            grassShader.SetBuffer(0, NAME_OF_TRIANGLE_BUFFER, triangleData);
            grassShader.SetBuffer(0, NAME_OF_GRASS_BUFFER, grassProperties);

            grassShader.Dispatch(0, numThreadPerAxis, 1,1);

            new InstanciableData(grassMesh, numTris, grassProperties, mat, bounds);
            grassProperties = null;
        }

    }
}