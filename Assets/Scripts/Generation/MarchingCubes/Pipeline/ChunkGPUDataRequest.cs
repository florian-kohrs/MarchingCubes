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

        //TODO: Inform about Mesh subset and mesh set vertex buffer
        //Subset may be used to only change parts of the mesh -> dont need multiple mesh displayers with submeshes?
        public TriangleChunkHeap DispatchAndGetShaderData(CompressedMarchingCubeChunk chunk, Action<CompressedMarchingCubeChunk> SetChunkComponents, Action<ComputeBuffer> WorkOnNoise = null)
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

        public TriangleBuilder[] DispatchRebuildAround(ReducedMarchingCubesChunk chunk, ComputeShader rebuildShader, Action DoStuffBeforeReadback, Vector3Int threadsPerAxis)
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

        public void DispatchAndGetShaderDataAsync(CompressedMarchingCubeChunk chunk, Action<CompressedMarchingCubeChunk> SetChunkComponents, Action<TriangleChunkHeap> OnDataDone)
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