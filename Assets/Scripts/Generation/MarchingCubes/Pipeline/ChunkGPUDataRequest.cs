using System;
using System.Collections;
using System.Collections.Generic;
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


        //TODO: Inform about Mesh subset and mesh set vertex buffer
        //Subset may be used to only change parts of the mesh -> dont need multiple mesh displayers with submeshes?
        public TriangleChunkHeap DispatchAndGetShaderData(ICompressedMarchingCubeChunk chunk, Action<ICompressedMarchingCubeChunk> SetChunkComponents, Action WorkOnNoise = null)
        {
            PrepareDataRequest();

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
            ReturnToPool();
            return new TriangleChunkHeap(tris, 0, numTris);
        }

        public void DispatchAndGetShaderDataAsync(ICompressedMarchingCubeChunk chunk, Action<ICompressedMarchingCubeChunk> SetChunkComponents, Action<TriangleChunkHeap> OnDataDone, Action WorkOnNoise = null)
        {
            PrepareDataRequest();

            ValidateChunkProperties(chunk);
            noise.TryLoadOrGenerateNoise(chunk);
            bool storeNoise = noise.WorkOnNoiseMap(chunk, WorkOnNoise);
            chunkPipeline.DispatchPrepareCubesFromNoise(chunk);

            float[] test = ComputeBufferExtension.ReadBuffer<float>(gpuData.pointsBuffer);
            Vector2Int[] test2 = ComputeBufferExtension.ReadBuffer<Vector2Int>(gpuData.preparedTrisBuffer);

            if (storeNoise)
            {
                noise.StoreNoise(chunk);
            }

            ComputeBufferExtension.GetLengthOfAppendBufferAsync(gpuData.preparedTrisBuffer, gpuData.triCountBuffer, (numTris) =>
            {
                if (numTris <= 0)
                {
                    ReturnToPool();
                    OnDataDone(new TriangleChunkHeap(Array.Empty<TriangleBuilder>(), 0, numTris));
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

    }
}