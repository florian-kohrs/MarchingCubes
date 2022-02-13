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


        public void PrepareChunkToStoreMinDegreesIfNeeded(ICompressedMarchingCubeChunk chunk)
        {
            bool storeMinDegree = chunk.LOD <= 1 && !chunk.IsReady;
            if (storeMinDegree)
            {
                ComputeBuffer minDegreeBuffer = minDegreeBufferPool.GetBufferForShaders();
                chunk.MinDegreeBuffer = minDegreeBuffer;
            }
        }


        public void DispatchPrepareCubesFromNoise(ICompressedMarchingCubeChunk chunk)
        {
            pipeline.ApplyPrepareTrianglesForChunk(chunk);

            int numVoxelsPerAxis = chunk.PointsPerAxis - 1;

            int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / NoisePipeline.THREAD_GROUP_SIZE_PER_AXIS);

            pipeline.prepareTrisShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
        }


        public ComputeBuffer BuildPreparedCubes(ICompressedMarchingCubeChunk chunk, int numTris)
        {
            PrepareChunkToStoreMinDegreesIfNeeded(chunk);

            ComputeBuffer trianglesBuffer = pipeline.ApplyBuildTrianglesForChunkProperties(chunk, numTris);

            int numThreads = Mathf.CeilToInt(numTris / THREAD_GROUP_SIZE);

            pipeline.buildTrisShader.Dispatch(0, numThreads, 1, 1);

            return trianglesBuffer;
        }

        public int ComputeCubesFromNoise(ICompressedMarchingCubeChunk chunk, out ComputeBuffer triangleBuffer)
        {
            DispatchPrepareCubesFromNoise(chunk);
            int numTris = ComputeBufferExtension.GetLengthOfAppendBuffer(pipeline.preparedTrisBuffer, pipeline.triCountBuffer);
            if (numTris > 0)
            {
                triangleBuffer = BuildPreparedCubes(chunk, numTris);
            }
            else
            {
                triangleBuffer = null;
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

        public int ComputeCubesFromNoiseAround(ICompressedMarchingCubeChunk chunk, ComputeShader rebuildShader, Vector3Int threadsPerAxis, out ComputeBuffer triangleBuffer)
        {
            DispatchPrepareCubesFromNoiseAround(rebuildShader, threadsPerAxis);
            int numTris = ComputeBufferExtension.GetLengthOfAppendBuffer(pipeline.preparedTrisBuffer, pipeline.triCountBuffer);
            if (numTris > 0)
            {
                triangleBuffer = BuildPreparedCubes(chunk, numTris);
            }
            else
            {
                triangleBuffer = null;
            }
            return numTris;
        }

    }
}