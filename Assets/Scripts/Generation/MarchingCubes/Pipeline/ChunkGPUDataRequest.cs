using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGPUDataRequest
    {

        public ChunkGPUDataRequest(ChunkGenerationPipelinePool pipelinePool, StorageGroupMesh storedNoiseEdits, BufferPool minDegreeBufferPool)
        {
            this.pipelinePool = pipelinePool;
            this.storedNoiseEdits = storedNoiseEdits;
            this.minDegreeBufferPool = minDegreeBufferPool;
            if(emptyMinDegreeBuffer == null)
            {
                emptyMinDegreeBuffer = minDegreeBufferPool.GetItemFromPool();
            }
        }

        public static ComputeBuffer emptyMinDegreeBuffer;


        public ChunkGenerationPipelinePool pipelinePool;

        public StorageGroupMesh storedNoiseEdits;

        public BufferPool minDegreeBufferPool;

      
        public void ValidateChunkProperties(ICompressedMarchingCubeChunk chunk)
        {
            if (chunk.ChunkSize % chunk.LOD != 0)
                throw new Exception("Lod must be divisor of chunksize");
        }

        public float[] RequestNoiseForChunk(ICompressedMarchingCubeChunk chunk)
        {
            ChunkGenerationGPUData gpuData = pipelinePool.GetItemFromPool();
            NoisePipeline noise = new NoisePipeline(gpuData, storedNoiseEdits);
            float[] result = noise.RequestNoiseForChunk(chunk);
            pipelinePool.ReturnItemToPool(gpuData);

            return result;
        }


        //TODO: Inform about Mesh subset and mesh set vertex buffer
        //Subset may be used to only change parts of the mesh -> dont need multiple mesh displayers with submeshes?
        public TriangleChunkHeap DispatchAndGetShaderData(ICompressedMarchingCubeChunk chunk, Action<ICompressedMarchingCubeChunk> SetChunkComponents, Action<ComputeBuffer> WorkOnNoise = null)
        {
            ChunkGenerationGPUData gpuData = pipelinePool.GetItemFromPool();
            NoisePipeline noise = new NoisePipeline(gpuData, storedNoiseEdits);
            ChunkPipeline chunkPipeline = new ChunkPipeline(gpuData, minDegreeBufferPool);

            ComputeBuffer triangleBuffer;

            ValidateChunkProperties(chunk);
            noise.TryLoadOrGenerateNoise(chunk);
            bool storeNoise = noise.WorkOnNoiseMap(chunk, WorkOnNoise);
            int numTris = chunkPipeline.ComputeCubesFromNoise(chunk, out triangleBuffer);

            TriangleBuilder[] tris;

            ///read data from gpu
            if (numTris == 0)
            {
                tris = Array.Empty<TriangleBuilder>();
            }
            else
            {
                SetChunkComponents(chunk);
                tris = new TriangleBuilder[numTris];
                triangleBuffer.GetData(tris);
                triangleBuffer.Dispose();
            }

            if (storeNoise)
            {
                noise.StoreNoise(chunk);
            }
            pipelinePool.ReturnItemToPool(gpuData);
            return new TriangleChunkHeap(tris, 0, numTris);
        }

        public TriangleBuilder[] DispatchRebuildAround(IMarchingCubeChunk chunk, ComputeShader rebuildShader, Action DoStuffBeforeReadback, Vector3Int threadsPerAxis)
        {
            ChunkGenerationGPUData gpuData = pipelinePool.GetItemFromPool();
            gpuData.pointsBuffer.SetData(chunk.Points);

            ChunkPipeline chunkPipeline = new ChunkPipeline(gpuData, minDegreeBufferPool);
            ComputeBuffer triangleBuffer;

            int numTris = chunkPipeline.ComputeCubesFromNoiseAround(chunk, rebuildShader, threadsPerAxis, out triangleBuffer);

            TriangleBuilder[] tris;

            ///read data from gpu
            if (numTris == 0)
            {
                tris = Array.Empty<TriangleBuilder>();
            }
            else
            {
                DoStuffBeforeReadback();
                tris = new TriangleBuilder[numTris];
                triangleBuffer.GetData(tris);
                triangleBuffer.Dispose();
            }

            pipelinePool.ReturnItemToPool(gpuData);
            return tris;
        }

        public void DispatchAndGetShaderDataAsync(ICompressedMarchingCubeChunk chunk, Action<ICompressedMarchingCubeChunk> SetChunkComponents, Action<TriangleChunkHeap> OnDataDone)
        {
            ChunkGenerationGPUData gpuData = pipelinePool.GetItemFromPool();
            NoisePipeline noise = new NoisePipeline(gpuData, storedNoiseEdits);
            ChunkPipeline chunkPipeline = new ChunkPipeline(gpuData, minDegreeBufferPool);

            ValidateChunkProperties(chunk);
            noise.TryLoadOrGenerateNoise(chunk);
            chunkPipeline.DispatchPrepareCubesFromNoise(chunk);


            ComputeBufferExtension.GetLengthOfAppendBufferAsync(gpuData.preparedTrisBuffer, gpuData.triCountBuffer, (numTris) =>
            {
                if (numTris <= 0)
                {
                    pipelinePool.ReturnItemToPool(gpuData);
                    OnDataDone(new TriangleChunkHeap(Array.Empty<TriangleBuilder>(), 0, 0));
                    //OnDataDone(new GpuAsyncRequestResult());
                }
                else
                {
                    //totalTriBuild += numTris;

                    SetChunkComponents(chunk);

                    ComputeBuffer trianglesBuffer = chunkPipeline.BuildPreparedCubes(chunk, numTris);

                    ///read data from gpu
                    ReadCurrentTriangleDataAsync(trianglesBuffer, (tris) =>
                    {
                        trianglesBuffer.Dispose();
                        pipelinePool.ReturnItemToPool(gpuData);
                        OnDataDone(new TriangleChunkHeap(tris.ToArray(), 0, numTris));
                        //OnDataDone(new GpuAsyncRequestResult(tris));
                    });
                }
            });
        }

        protected void ReadCurrentTriangleDataAsync(ComputeBuffer triangleBuffer, Action<NativeArray<TriangleBuilder>> callback)
        {
            ComputeBufferExtension.ReadBufferAsync(triangleBuffer, callback);
        }

    }
}