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
        }

   
        public static void AssignEmptyMinDegreeBuffer(ComputeBuffer buffer)
        {
            emptyMinDegreeBuffer = buffer;
        }

        public static void DisposeEmptyMinDegreeBuffer()
        {
            emptyMinDegreeBuffer.Dispose();
        }

        public static ComputeBuffer emptyMinDegreeBuffer;


        public ChunkGenerationPipelinePool pipelinePool;

        public StorageGroupMesh storedNoiseEdits;

        public BufferPool minDegreeBufferPool;

      
        public void ValidateChunkProperties(CompressedMarchingCubeChunk chunk)
        {
            if (chunk.ChunkSize % chunk.LOD != 0)
                throw new Exception("Lod must be divisor of chunksize");
        }

        public float[] RequestNoiseForChunk(CompressedMarchingCubeChunk chunk)
        {
            ChunkGenerationGPUData gpuData = pipelinePool.GetItemFromPool();
            NoisePipeline noise = new NoisePipeline(gpuData, storedNoiseEdits);
            float[] result = noise.RequestNoiseForChunk(chunk);
            pipelinePool.ReturnItemToPool(gpuData);

            return result;
        }

        public Vector3Int[] ScanForNonEmptyChunksAround(Vector3 position, int sizePower, int lodPower)
        {
            CompressedMarchingCubeChunk emptyChunk = new CompressedMarchingCubeChunk()
            {
                PointSpacing = 1,
                LODPower = lodPower,
                NodeSizePower = sizePower,
            };
            float offset = emptyChunk.ChunkSize;
            emptyChunk.AnchorPos = VectorExtension.ToVector3Int(position - new Vector3(offset / 2, offset / 2, offset / 2));


            ChunkGenerationGPUData gpuData = pipelinePool.GetItemFromPool();
            NoisePipeline noise = new NoisePipeline(gpuData, storedNoiseEdits);
            ChunkPipeline chunkPipeline = new ChunkPipeline(gpuData, minDegreeBufferPool);

            noise.DispatchNoiseForChunk(emptyChunk,false);
            chunkPipeline.DispatchFindNonEmptyChunks(emptyChunk);
            Vector3Int[] nonEmptyPositions = ComputeBufferExtension.ReadAppendBuffer<Vector3Int>(ChunkGenerationGPUData.chunkPositionBuffer, gpuData.triCountBuffer);
            pipelinePool.ReturnItemToPool(gpuData);
            return nonEmptyPositions;
        }

        public MeshData DispatchAndGetChunkMeshData(CompressedMarchingCubeChunk chunk, Action<CompressedMarchingCubeChunk> SetChunkComponents, Action<ComputeBuffer> WorkOnNoise = null)
        {
            ChunkGenerationGPUData gpuData = pipelinePool.GetItemFromPool();
            NoisePipeline noise = new NoisePipeline(gpuData, storedNoiseEdits);
            ChunkPipeline chunkPipeline = new ChunkPipeline(gpuData, minDegreeBufferPool);

            ComputeBuffer vertsBuffer;
            ComputeBuffer colorBuffer;

            ValidateChunkProperties(chunk);
            noise.TryLoadOrGenerateNoise(chunk);
            bool storeNoise = noise.WorkOnNoiseMap(chunk, WorkOnNoise);
            int numTris = chunkPipeline.ComputeMeshDataFromNoise(chunk, out vertsBuffer, out colorBuffer);


            Vector3[] verts;
            Color32[] colors;

            ///read data from gpu
            if (numTris == 0)
            {
                verts = Array.Empty<Vector3>();
                colors = Array.Empty<Color32>();
            }
            else
            {
                SetChunkComponents?.Invoke(chunk);
                verts = new Vector3[numTris * 3];
                colors = new Color32[numTris * 3];
                vertsBuffer.GetData(verts);
                colorBuffer.GetData(colors);
                vertsBuffer.Dispose();
                colorBuffer.Dispose();
            }

            if (storeNoise)
            {
                noise.StoreNoise(chunk);
            }
            pipelinePool.ReturnItemToPool(gpuData);
            return new MeshData(verts, colors, chunk.UseCollider);
        }

        public void DispatchAndGetChunkMeshDataAsync(CompressedMarchingCubeChunk chunk, Action<CompressedMarchingCubeChunk> SetChunkComponents, Action<MeshData> onMeshDataDone)
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
                    onMeshDataDone(new MeshData(null, null, false));
                }
                else
                {
                    //totalTriBuild += numTris;

                    SetChunkComponents(chunk);
                    ComputeBuffer verts;
                    ComputeBuffer colors;
                    chunkPipeline.BuildMeshFromPreparedCubes(chunk, numTris, out verts, out colors);

                    ///read data from gpu
                    ComputeBufferExtension.ReadBuffersAsync<Vector3, Color32>(verts, colors, (vs, cs) =>
                    {
                        verts.Dispose();
                        colors.Dispose();
                        pipelinePool.ReturnItemToPool(gpuData);
                        onMeshDataDone(new MeshData(vs, cs, chunk.UseCollider));
                        //OnDataDone(new GpuAsyncRequestResult(tris));
                    });
                }
            });
        }

    }
}