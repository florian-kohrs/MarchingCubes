using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MarchingCubes;
using UnityEngine.Rendering;
using System;
using Unity.Collections;
using System.Linq;

namespace MeshGPUInstanciation
{
    public class EnvironmentSpawner : MonoBehaviour
    {

        protected const int BUFFER_CHUNK_SIZE =
            MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE *
            MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE *
            MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE;

        protected const int NUM_THREADS_PER_AXIS = 4;
        protected const int THREADS_PER_AXIS = MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE / NUM_THREADS_PER_AXIS;
        protected const int MAX_ENVIRONMENT_ENTITIES = MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE * MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE * 3;


        protected const string BOUNDS_CENTER_NAME = "boundsCenter";
        protected const string MESH_HEIGHT_EXTENSION_NAME = "meshHeightExtends";


        public Mesh mesh;

        public Material mat;



        public ComputeShader environmentSpawner;

        public ComputeShader environmentPlacer;

        public const float ENVIRONMENT_THREAD_SIZE = 32;

        public ComputeShader GrassSpawner;

        /// <summary>
        /// at which chunk coord is which environment entity (trees, etc.) located
        /// </summary>
        protected ComputeBuffer environmentEntities;

        /// <summary>
        /// buffer where all transforms are stored in
        /// </summary>
        protected ComputeBuffer entityTransforms;

        protected ComputeBuffer bufferCount;

        /// <summary>
        /// set which contains the indices of all original cubes positions => used to figure out if detailed
        /// environment can spawn on triangle
        /// </summary>
        protected ComputeBuffer originalCubeSet;

        /// <summary>
        /// set where no detailed environment will be spawned cause its occupied
        /// </summary>
        protected ComputeBuffer isTreeAtCube;

        protected struct EnvironmentData
        {
            public int cube;
        }

        //TODO: When saving a chunk save all positions where trees are

        private void Awake()
        {
            CreateBuffers();
        }

        protected void CreateBuffers()
        {
            bufferCount = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            isTreeAtCube = new ComputeBuffer(BUFFER_CHUNK_SIZE, sizeof(int));
            environmentEntities = new ComputeBuffer(MAX_ENVIRONMENT_ENTITIES, sizeof(int), ComputeBufferType.Append);
            environmentSpawner.SetBuffer(0, "entitiesAtCube", environmentEntities);
            environmentPlacer.SetBuffer(0, "entitiesAtCube", environmentEntities);
            environmentPlacer.SetFloat(MESH_HEIGHT_EXTENSION_NAME, mesh.bounds.extents.y);
        }

        protected void PrepareEnvironmentForChunk(IEnvironmentSurface chunk)
        {
            environmentEntities.SetCounterValue(0);
            environmentSpawner.SetBuffer(0, "minAngleAtCubeIndex", chunk.MinDegreeBuffer);
            Vector4 v4 = VectorExtension.ToVector4(chunk.AnchorPos);
            environmentSpawner.SetVector("anchorPosition", v4);
            environmentPlacer.SetVector("anchorPosition", v4);
            environmentPlacer.SetVector(BOUNDS_CENTER_NAME, chunk.MeshBounds.Value.center);
            if (chunk.BuildDetailedEnvironment)
            {

            }
        } 


        //TODO: Use prepare tri buffer to place environment. also do this async
        public void AddEnvironmentForOriginalChunk(IEnvironmentSurface chunk)
        {
            //float[] mindegs = ReadBuffer<float>(chunk.MinDegreeBuffer);

            //float[] nonNullDegs = mindegs.Where(f => f > 0).ToArray();

            PrepareEnvironmentForChunk(chunk);
            environmentSpawner.Dispatch(0, THREADS_PER_AXIS, THREADS_PER_AXIS, THREADS_PER_AXIS);
            int entityCount = ComputeBufferExtension.GetLengthOfAppendBuffer(environmentEntities,bufferCount);
            if (entityCount <= 0)
                return;

            entityTransforms = new ComputeBuffer(entityCount, sizeof(float) * 16);
            environmentPlacer.SetBuffer(0, "entityTransform", entityTransforms);
            environmentPlacer.SetInt("length", entityCount);
            int threadsOnXAxis = Mathf.CeilToInt(entityCount / ENVIRONMENT_THREAD_SIZE);
            environmentPlacer.Dispatch(0, threadsOnXAxis, 1, 1);
            MeshInstantiator.meshInstantiator.AddData(new InstantiatableData(mesh, entityTransforms, mat, chunk.MeshBounds, entityCount));

            //Debug test data
            Matrix4x4[] test = new Matrix4x4[entityCount];
            entityTransforms.GetData(test);
            int[] results = ComputeBufferExtension.ReadAppendBuffer<int>(environmentEntities, bufferCount);
            int i = 0;


            //AsyncGPUReadback.Request(environmentEntities, OnTreePositionsRecieved);

            //recieve tree positions
            //recieve tree rotations -> place colliders from pool
            //add grass to unused cubes
            //when editing chunk store tree positions and rotations and original cubes

        }



        protected void ComputeGrassForChunk(CompressedMarchingCubeChunk chunk)
        {

        }


        public void AddEnvirenmentForEditedChunk(CompressedMarchingCubeChunk chunk, bool buildDetailEnvironment)
        {
            environmentSpawner.SetBuffer(0, "minAngleAtCubeIndex", chunk.MinDegreeBuffer);
            environmentSpawner.SetVector("anchorPosition", VectorExtension.ToVector4(chunk.AnchorPos));
            environmentSpawner.Dispatch(0, THREADS_PER_AXIS, THREADS_PER_AXIS, THREADS_PER_AXIS);
            int[] results = ComputeBufferExtension.ReadAppendBuffer<int>(environmentEntities, bufferCount);

            //AsyncGPUReadback.Request(environmentEntities, OnTreePositionsRecieved);

            //recieve tree positions
            //recieve tree rotations -> place colliders from pool
            //add grass to unused cubes
            //when editing chunk store tree positions and rotations and original cubes
        }



        protected void OnTreePositionsRecieved(AsyncGPUReadbackRequest result)
        {
            NativeArray<Matrix4x4> x = result.GetData<Matrix4x4>();
            int length = x.Length;
            for (int i = 0; i < length; i++)
            {
                Matrix4x4 transform = x[i];
                //place collider there
            }
        }

        private void OnDestroy()
        {
            environmentEntities.SetCounterValue(0);
            bufferCount.Dispose();
            environmentEntities.Dispose();
            //originalCubeSet.Dispose();
            isTreeAtCube.Dispose();
        }

    }
}