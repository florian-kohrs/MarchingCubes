using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGPUDataRequest
    {

        protected const float THREAD_GROUP_SIZE = 32;

        public ChunkGPUDataRequest(ChunkGenerationPipelinePool pipelinePool, StorageGroupMesh storedNoiseEdits, BufferPool minDegreeBufferPool)
        {
            this.pipelinePool = pipelinePool;
            this.storedNoiseEdits = storedNoiseEdits;
            this.minDegreeBufferPool = minDegreeBufferPool;
        }


        public ChunkGenerationPipelinePool pipelinePool;

        public StorageGroupMesh storedNoiseEdits;

        public BufferPool minDegreeBufferPool;


        protected ChunkGenerationGPUData gpuData;

        protected NoisePipeline noise;

        protected ChunkPipeline chunkPipeline;

      
        protected void PrepareDataRequest()
        {
            gpuData = pipelinePool.GetItemFromPool();
            noise = new NoisePipeline(gpuData, storedNoiseEdits);
            chunkPipeline = new ChunkPipeline(gpuData, minDegreeBufferPool);
        }

        protected void ReturnToPool()
        {
            noise = null;
            chunkPipeline = null;
            pipelinePool.ReturnItemToPool(gpuData);
        }

        public void ValidateChunkProperties(ICompressedMarchingCubeChunk chunk)
        {
            if (chunk.ChunkSize % chunk.LOD != 0)
                throw new Exception("Lod must be divisor of chunksize");
        }

        protected TriangleChunkHeap DispatchAndGetShaderData(ICompressedMarchingCubeChunk chunk, Action WhileWaitOnGpuResult, Action WorkOnNoise = null)
        {
            PrepareDataRequest();

            ValidateChunkProperties(chunk);
            noise.TryLoadOrGenerateNoise(chunk);
            bool storeNoise = noise.WorkOnNoiseMap(chunk, WorkOnNoise);
            int numTris = ComputeCubesFromNoise(chunk);
            ///Do work for chunk here, before data from gpu is read, to give gpu time to finish

            WhileWaitOnGpuResult();
            //SetDisplayerOfChunk(chunk);
            //SetLODColliderOfChunk(chunk);

            TriangleBuilder[] tris = new TriangleBuilder[numTris];
            ///read data from gpu
            ReadCurrentTriangleData(tris);
            if (numTris == 0)
            {
                chunk.FreeSimpleChunkCollider();
                chunk.GiveUnusedDisplayerBack();
            }

            if (storeNoise)
            {
                StoreNoise(chunk, noiseBuffer);
            }
            else if (numTris == 0 && !hasFoundInitialChunk)
            {
                DetermineIfChunkIsAir(noiseBuffer);
            }
            ReturnToPool();
            return new TriangleChunkHeap(tris, 0, numTris);
        }

        public void DispatchAndGetShaderDataAsync(ICompressedMarchingCubeChunk chunk, Action<TriangleChunkHeap> OnDataDone, Action WorkOnNoise = null)
        {
            PrepareDataRequest();

            ValidateChunkProperties(chunk);
            noise.TryLoadOrGenerateNoise(chunk);
            bool storeNoise = noise.WorkOnNoiseMap(chunk, WorkOnNoise);
            chunkPipeline.DispatchCubesFromNoise(chunk);

            if (storeNoise)
            {
                //NoisePipeline.StoreNoise(chunk, noiseBuffer);
            }

            ComputeBufferExtension.GetLengthOfAppendBufferAsync(gpuData.preparedTrisBuffer, gpuData.triCountBuffer, (numTris) =>
            {
                if (numTris <= 0)
                {
                    //Do this on callback function
                    //if (!hasFoundInitialChunk)
                    //{
                    //    DetermineIfChunkIsAir(noiseBuffer);
                    //}
                    //preparedTrianglePool.ReturnItemToPool(trianglesToBuild);
                    //pointsBufferPool.ReturnItemToPool(noiseBuffer);
                    ReturnToPool();
                    OnDataDone(new TriangleChunkHeap(Array.Empty<TriangleBuilder>(), 0, numTris));
                }
                else
                {
                    //totalTriBuild += numTris;
                
                    //SetDisplayerOfChunk(chunk);
                    //SetLODColliderOfChunk(chunk);

                    ComputeBuffer trianglesBuffer = new ComputeBuffer(numTris, TriangleBuilder.SIZE_OF_TRI_BUILD);
                    gpuData.buildTrisShader.SetBuffer(0, "triangles", trianglesBuffer);
                    BuildPreparedCubes(chunk, numTris);

                    ///read data from gpu
                    ReadCurrentTriangleDataAsync(trianglesBuffer, (tris) =>
                    {
                        trianglesBuffer.Dispose();
                        ReturnToPool();
                        //TODO:Remove to Array!!
                        OnDataDone(new TriangleChunkHeap(tris.ToArray(), 0, numTris));
                    });
                }
            });
        }

        protected void ReadCurrentTriangleDataAsync(ComputeBuffer triangleBuffer, Action<NativeArray<TriangleBuilder>> callback)
        {
            ComputeBufferExtension.ReadBufferAsync(triangleBuffer, callback);
        }

        protected void BuildPreparedCubes(ICompressedMarchingCubeChunk chunk, int numTris)
        {
            chunkPipeline.PrepareChunkToStoreMinDegreesIfNeeded(chunk);

            gpuData.ApplyBuildTrianglesForChunkProperties(chunk, numTris);

            int numThreads = Mathf.CeilToInt(numTris / THREAD_GROUP_SIZE);

            gpuData.buildTrisShader.Dispatch(0, numThreads, 1, 1);
        }
    }
}