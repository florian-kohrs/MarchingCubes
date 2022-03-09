using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkPipeline
    {


        public const float THREAD_GROUP_SIZE = 32;

        public ChunkPipeline(ChunkGenerationGPUData pipeline, BufferPool minDegreeBufferPool)
        {
            this.pipeline = pipeline;
            this.minDegreeBufferPool = minDegreeBufferPool;
        }

        protected ChunkGenerationGPUData pipeline;

        protected BufferPool minDegreeBufferPool;


        public void DispatchPrepareCubesFromNoise(CompressedMarchingCubeChunk chunk)
        {
            pipeline.ApplyPrepareTrianglesForChunk(chunk);

            int numVoxelsPerAxis = chunk.PointsPerAxis - 1;

            int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / NoisePipeline.THREAD_GROUP_SIZE_PER_AXIS);

            pipeline.prepareTrisShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
        }


        public void BuildMeshFromPreparedCubes(CompressedMarchingCubeChunk chunk, int numTris, out ComputeBuffer verts, out ComputeBuffer colors)
        {
            pipeline.ApplyBuildMeshDataPropertiesForChunk(chunk, numTris, out verts, out colors);

            int numThreads = Mathf.CeilToInt(numTris / THREAD_GROUP_SIZE);

            pipeline.buildMeshDataShader.Dispatch(0, numThreads, 1, 1);
        }

        public int ComputeMeshDataFromNoise(CompressedMarchingCubeChunk chunk, out ComputeBuffer verts, out ComputeBuffer colors)
        {
            DispatchPrepareCubesFromNoise(chunk);
            int numTris = ComputeBufferExtension.GetLengthOfAppendBuffer(pipeline.preparedTrisBuffer, pipeline.triCountBuffer);
            if (numTris > 0)
            {
                BuildMeshFromPreparedCubes(chunk, numTris, out verts, out colors);
            }
            else
            {
                colors = null;
                verts = null;
            }
            return numTris;
        }

        public void DispatchPrepareCubesFromNoiseAround(ComputeShader rebuildShader, Vector3Int threadsPerAxis)
        {
            rebuildShader.SetBuffer(0, "points", pipeline.pointsBuffer);
            rebuildShader.SetBuffer(0, "triangleLocations", pipeline.preparedTrisBuffer);
            pipeline.preparedTrisBuffer.SetCounterValue(0);

            rebuildShader.Dispatch(0, threadsPerAxis.x, threadsPerAxis.y, threadsPerAxis.z);
        }


    }
}