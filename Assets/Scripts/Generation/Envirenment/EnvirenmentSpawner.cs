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
    public class EnvirenmentSpawner : MonoBehaviour
    {

        protected const int BUFFER_CHUNK_SIZE =
            MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE *
            MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE *
            MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE;

        protected const int NUM_THREADS_PER_AXIS = 4;
        protected const int THREADS_PER_AXIS = MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE / NUM_THREADS_PER_AXIS;
        protected const int MAX_ENVIRONMENT_ENTITIES = MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE * MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE * 3;

        public ComputeShader EnvironmentPlacer;


        public ComputeShader GrassSpawner;

        /// <summary>
        /// at which chunk coord is which environment entity (trees, etc.) located
        /// </summary>
        protected ComputeBuffer environmentEntities;

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
            EnvironmentPlacer.SetBuffer(0, "entitiesAtCube", environmentEntities);
        }

        protected void PrepareEnvironmentForChunk(IEnvironmentSurface chunk)
        {
            environmentEntities.SetCounterValue(0);
            EnvironmentPlacer.SetBuffer(0, "minAngleAtCubeIndex", chunk.MinDegreeBuffer);
            EnvironmentPlacer.SetVector("anchorPosition", VectorExtension.RaiseVector3Int(chunk.AnchorPos));
            if (chunk.BuildDetailedEnvironment)
            {

            }
        }

        public void AddEnvironmentForOriginalChunk(IEnvironmentSurface chunk)
        {
            float[] mindegs = ReadBuffer<float>(chunk.MinDegreeBuffer);

            float[] nonNullDegs = mindegs.Where(f => f > 0).ToArray();

            PrepareEnvironmentForChunk(chunk);
            EnvironmentPlacer.Dispatch(0, THREADS_PER_AXIS, THREADS_PER_AXIS, THREADS_PER_AXIS);
            int[] results = ReadAppendBuffer<int>(environmentEntities);
            int i = 0;
            //AsyncGPUReadback.Request(environmentEntities, OnTreePositionsRecieved);

            //recieve tree positions
            //recieve tree rotations -> place colliders from pool
            //add grass to unused cubes
            //when editing chunk store tree positions and rotations and original cubes

        }

        protected void ComputeGrassForChunk(IMarchingCubeChunk chunk)
        {

        }


        public void AddEnvirenmentForEditedChunk(IMarchingCubeChunk chunk, bool buildDetailEnvironment)
        {
            EnvironmentPlacer.SetBuffer(0, "minAngleAtCubeIndex", chunk.MinDegreeBuffer);
            EnvironmentPlacer.SetVector("anchorPosition", VectorExtension.RaiseVector3Int(chunk.AnchorPos));
            EnvironmentPlacer.Dispatch(0, THREADS_PER_AXIS, THREADS_PER_AXIS, THREADS_PER_AXIS);
            int[] results = ReadAppendBuffer<int>(environmentEntities);

            //AsyncGPUReadback.Request(environmentEntities, OnTreePositionsRecieved);

            //recieve tree positions
            //recieve tree rotations -> place colliders from pool
            //add grass to unused cubes
            //when editing chunk store tree positions and rotations and original cubes
        }

        protected T[] ReadAppendBuffer<T>(ComputeBuffer buffer)
        {
            T[] result;
            ComputeBuffer.CopyCount(buffer, bufferCount, 0);
            int[] length = new int[1];
            bufferCount.GetData(length);
            result = new T[length[0]];
            buffer.GetData(result);
            return result;
        }


        protected T[] ReadBuffer<T>(ComputeBuffer buffer)
        {
            T[] result = new T[BUFFER_CHUNK_SIZE];
            buffer.GetData(result);
            return result;
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

            environmentEntities.Dispose();
            //originalCubeSet.Dispose();
            isTreeAtCube.Dispose();
        }

    }
}