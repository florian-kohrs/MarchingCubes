using MarchingCubes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshGPUInstanciation
{
    public class SpawnGrassForMarchingCube : MonoBehaviour
    {

        protected const int MAX_VOXELS = 
            MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE *
            MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE *
            MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE;

        protected static string NAME_OF_TRIANGLE_BUFFER = "meshTriangles";
        protected static string NAME_OF_OFFSET = "offset";
        protected static string NAME_OF_BOUNDS = "boundsHeight";
        protected static string NAME_OF_TRIANGLE_LENGTH = "length";
        protected static string NAME_OF_GRASS_BUFFER = "grassPositions";
        protected static string NAME_OF_START_INDEX = "startIndex";

        [System.NonSerialized]
        protected ComputeBuffer grassProperties;

        protected ComputeBuffer triangleBuffer;

        private void Awake()
        {
            triangleBuffer = new ComputeBuffer(MAX_VOXELS, TriangleBuilder.SIZE_OF_TRI_BUILD);
        }

        public ComputeShader grassShader;
        public Material mat;
        public Mesh grassMesh;


        public const int GRASS_PER_COMPUTE = 32 * 32 * 5 * 4;

        public void ComputeGrassFor(IEnvironmentSurface chunk)
        {
            TriangleChunkHeap tris = chunk.ChunkHeap;
            int numTris = tris.triCount;
            triangleBuffer.SetData(tris.tris);

            int numThreadPerAxis = Mathf.Max(1,Mathf.CeilToInt(numTris / 32f));
            grassProperties = new ComputeBuffer(GRASS_PER_COMPUTE, MeshInstancedProperties.Size(), ComputeBufferType.Append);
            grassProperties.SetCounterValue(0);
            Material mat = new Material(this.mat);
            Maybe<Bounds> mBounds = chunk.MeshBounds;
            Vector3 offset = mBounds.Value.center;

            grassShader.SetInt(NAME_OF_TRIANGLE_LENGTH, numTris);
            grassShader.SetInt(NAME_OF_START_INDEX, tris.startIndex);
            grassShader.SetVector(NAME_OF_OFFSET, offset);
            grassShader.SetFloat(NAME_OF_BOUNDS, grassMesh.bounds.extents.y);
            grassShader.SetBuffer(0, NAME_OF_TRIANGLE_BUFFER, triangleBuffer);
            grassShader.SetBuffer(0, NAME_OF_GRASS_BUFFER, grassProperties);

            grassShader.Dispatch(0, numThreadPerAxis, 1, 1);

            new InstantiatableData(grassMesh, grassProperties, mat, mBounds);
            grassProperties = null;
        }

        private void OnDestroy()
        {
            triangleBuffer.Dispose();
        }


    }
}