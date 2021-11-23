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

        protected InstanciableData grass;
        protected ComputeBuffer triData;
        protected ComputeBuffer grassProperties;
        public ComputeShader grassShader;
        public Material mat;
        public Mesh grassMesh;

     
        public void ComputeGrassFor(Bounds bounds, int numTris, ComputeBuffer triangleData)
        {
            int numThreadPerAxis = Mathf.CeilToInt(numTris / 32f);
            grassShader.SetInt(NAME_OF_TRIANGLE_LENGTH, numTris);
            grassShader.SetBuffer(0, NAME_OF_TRIANGLE_BUFFER, triangleData);
            grassShader.SetBuffer(0, NAME_OF_GRASS_BUFFER, grassProperties);
            //grassShader.Dispatch(0, numThreadPerAxis, 1, 1);
            grass = new InstanciableData(grassMesh,grassProperties, mat, bounds);
            grass = null;
        }


        private void Start()
        {
            grassProperties = new ComputeBuffer(32 * 32 * 10, MeshInstancedProperties.Size(), ComputeBufferType.Append);
            triData = new ComputeBuffer(1, MarchingCubes.TriangleBuilder.SIZE_OF_TRI_BUILD);
            MarchingCubes.TriangleBuilder t = new MarchingCubes.TriangleBuilder(new MarchingCubes.Triangle(Vector3.one * 2,default,default));
            
            triData.SetData(new MarchingCubes.TriangleBuilder[] { t });
            ComputeGrassFor(new Bounds(Vector2.zero, Vector3.one * 5), 1, triData);
        }

        private void OnDestroy()
        {
            triData.Dispose();
        }

    }
}