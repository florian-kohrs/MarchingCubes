using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class NoisePipeline 
    {

        public const float THREAD_GROUP_SIZE_PER_AXIS = 4;

        public NoisePipeline(ChunkGenerationGPUData pipeline, StorageGroupMesh storageGroup)
        {
            this.storageGroup = storageGroup;
            this.pipeline = pipeline;
        }

        protected ChunkGenerationGPUData pipeline;

        public StorageGroupMesh storageGroup;

        public void StoreNoise(CompressedMarchingCubeChunk chunk)
        {
            int pointsPerAxis = chunk.PointsPerAxis;
            int pointsVolume = pointsPerAxis * pointsPerAxis * pointsPerAxis;
            float[] pointsArray = new float[pointsVolume];
            pipeline.pointsBuffer.GetData(pointsArray);
            if (chunk is ReducedMarchingCubesChunk c)
            {
                c.Points = pointsArray;
                storageGroup.Store(chunk.AnchorPos, chunk as ReducedMarchingCubesChunk, true);
            }
        }

        public float[] GenerateAndGetNoiseForChunk(CompressedMarchingCubeChunk chunk)
        {
            float[] result;
            int pointsPerAxis = chunk.PointsPerAxis;
            TryLoadOrGenerateNoise(chunk);
            result = new float[pointsPerAxis * pointsPerAxis * pointsPerAxis];
            pipeline.pointsBuffer.GetData(result, 0, 0, result.Length);
            return result;
        }


        public float[] RequestNoiseForChunk(CompressedMarchingCubeChunk chunk)
        {
            float[] result;
            if (!storageGroup.TryLoadCompleteNoise(chunk.AnchorPos, chunk.ChunkSizePower, out result, out bool _))
            {
                result = GenerateAndGetNoiseForChunk(chunk);
            }
            return result;
        }

        public void TryLoadOrGenerateNoise(CompressedMarchingCubeChunk chunk)
        {
            bool hasStoredData = false;
            bool isMipMapComplete = false;
            int sizePow = chunk.ChunkSizePower;
            float[] storedNoiseData = null;
            if (chunk.LODPower <= MarchingCubeChunkHandler.STORAGE_GROUP_UNTIL_LOD && sizePow <= MarchingCubeChunkHandler.STORAGE_GROUP_SIZE_POWER)
            {
                hasStoredData = storageGroup.TryGetMipMapAt(chunk.AnchorPos, sizePow, out storedNoiseData, out isMipMapComplete);
                if (hasStoredData && (!isMipMapComplete))
                {
                    pipeline.savedPointsBuffer.SetData(storedNoiseData);
                }
            }
            if (isMipMapComplete)
            {
                int pointSpacing = (int)Mathf.Pow(2, chunk.LODPower - (chunk.ChunkSizePower - MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE_POWER));

                chunk.PointSpacing = pointSpacing;
                pipeline.pointsBuffer.SetData(storedNoiseData);
            }
            else
            {
                DispatchNoiseForChunk(chunk, hasStoredData);
            }
        }

        protected void DispatchNoiseForChunk(CompressedMarchingCubeChunk chunk, bool hasStoredData)
        {
            int groupsPerAxis = Mathf.CeilToInt(chunk.PointsPerAxis / THREAD_GROUP_SIZE_PER_AXIS);
            pipeline.ApplyDensityPropertiesForChunk(chunk, hasStoredData);
            pipeline.densityGeneratorShader.Dispatch(0, groupsPerAxis, groupsPerAxis, groupsPerAxis);
            float[] noise = new float[chunk.PointsPerAxis * chunk.PointsPerAxis * chunk.PointsPerAxis];
            pipeline.pointsBuffer.GetData(noise);
        }

        public bool WorkOnNoiseMap(CompressedMarchingCubeChunk chunk, Action<ComputeBuffer> a)
        {
            bool storeNoise = false;
            if (a != null)
            {
                if (!(chunk is ReducedMarchingCubesChunk))
                {
                    throw new ArgumentException("Chunk has to be storeable to be able to store requested noise!");
                }
                a(pipeline.pointsBuffer);
                storeNoise = true;
            }
            return storeNoise;
        }

    }
}