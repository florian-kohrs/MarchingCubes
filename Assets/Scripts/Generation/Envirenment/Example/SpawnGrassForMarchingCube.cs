using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshGPUInstanciation
{
    public class SpawnGrassForMarchingCube : MonoBehaviour
    {

        protected static string NAME_OF_TRIANGLE_BUFFER = "meshTriangles";
        protected static string NAME_OF_TRIANGLE_LENGTH = "length";
        protected static string NAME_OF_GRASS_BUFFER = "grassPositions";

        [System.NonSerialized]
        protected ComputeBuffer grassProperties;

        public ComputeShader grassShader;
        public Material mat;
        public Mesh grassMesh;

        protected ComputeBuffer GrassProperties
        {
            get
            {
                if(grassProperties==null)
                {
                    grassProperties = new ComputeBuffer(32 * 32 * 10, MeshInstancedProperties.Size(), ComputeBufferType.Append);
                }
                return grassProperties;
            }
        }

        public void ComputeGrassFor(Bounds bounds, int numTris, ComputeBuffer triangleData)
        {
            int numThreadPerAxis = Mathf.CeilToInt(numTris / 32f);
            GrassProperties.SetCounterValue(0);


            grassShader.SetInt(NAME_OF_TRIANGLE_LENGTH, numTris);
            grassShader.SetBuffer(0, NAME_OF_TRIANGLE_BUFFER, triangleData);
            grassShader.SetBuffer(0, NAME_OF_GRASS_BUFFER, GrassProperties);

            grassShader.Dispatch(0, numThreadPerAxis, 1,1);

            new InstanciableData(grassMesh, numTris, grassProperties, mat, bounds);
        }

        private void OnDestroy()
        {
            GrassProperties.Dispose();
        }

    }
}