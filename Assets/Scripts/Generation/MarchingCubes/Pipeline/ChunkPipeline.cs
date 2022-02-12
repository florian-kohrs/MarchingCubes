using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkPipeline
    {

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
            pipeline.buildTrisShader.SetBool("storeMinDegrees", storeMinDegree);
            if (storeMinDegree)
            {
                ComputeBuffer minDegreeBuffer = minDegreeBufferPool.GetBufferForShaders();
                chunk.MinDegreeBuffer = minDegreeBuffer;
            }
        }


        public void DispatchCubesFromNoise(ICompressedMarchingCubeChunk chunk)
        {
            pipeline.ApplyPrepareTrianglesForChunk(chunk);

            int numVoxelsPerAxis = chunk.PointsPerAxis - 1;

            int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / NoisePipeline.THREAD_GROUP_SIZE_PER_AXIS);

            pipeline.prepareTrisShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
        }


        public int ComputeCubesFromNoise(ICompressedMarchingCubeChunk chunk)
        {
            DispatchCubesFromNoise(chunk);
            int numTris = ComputeBufferExtension.GetLengthOfAppendBuffer(pipeline.preparedTrisBuffer, pipeline.triCountBuffer);

            BuildPreparedCubes(chunk, trisToBuildBuffer, numTris);

            return numTris;
        }

    }
}