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

        protected const float THREAD_SIZE_GROUP = 32;

        protected static string NAME_OF_TRIANGLE_BUFFER = "meshTriangles";
        protected static string NAME_OF_OFFSET = "offset";
        protected static string NAME_OF_BOUNDS = "boundsHeight";
        protected static string NAME_OF_TRIANGLE_LENGTH = "length";
        protected static string NAME_OF_GRASS_BUFFER = "grassPositions";

        [System.NonSerialized]
        protected ComputeBuffer grassProperties;

        protected ComputeBuffer vertexBuffer;

        private void Awake()
        {
            vertexBuffer = new ComputeBuffer(MAX_VOXELS * 6, sizeof(float) * 3);
        }

        public ComputeShader grassShader;
        public Material mat;
        public Mesh grassMesh;


        public const int GRASS_PER_COMPUTE = 32 * 32 * 5 * 4;

        public void ComputeGrassFor(IEnvironmentSurface chunk)
        {
            MeshData meshData = chunk.MeshData.Value;
            int numTris = meshData.vertices.Length / 3;
            vertexBuffer.SetData(meshData.vertices);

            int numThreadPerAxis = Mathf.Max(1,Mathf.CeilToInt(numTris / THREAD_SIZE_GROUP));
            grassProperties = new ComputeBuffer(GRASS_PER_COMPUTE, MeshInstancedProperties.Size(), ComputeBufferType.Append);
            grassProperties.SetCounterValue(0);
            Material mat = new Material(this.mat);
            Maybe<Bounds> mBounds = chunk.MeshBounds;
            Vector3 offset = mBounds.Value.center;

            grassShader.SetInt(NAME_OF_TRIANGLE_LENGTH, numTris);
            grassShader.SetVector(NAME_OF_OFFSET, offset);
            grassShader.SetFloat(NAME_OF_BOUNDS, grassMesh.bounds.extents.y);
            grassShader.SetBuffer(0, NAME_OF_TRIANGLE_BUFFER, vertexBuffer);
            grassShader.SetBuffer(0, NAME_OF_GRASS_BUFFER, grassProperties);

            grassShader.Dispatch(0, numThreadPerAxis, 1, 1);

            new InstantiatableData(grassMesh, grassProperties, mat, mBounds);
            grassProperties = null;
        }

        private void OnDestroy()
        {
            vertexBuffer.Dispose();
        }


    }
}