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

        public void StoreNoise(ICompressedMarchingCubeChunk chunk)
        {
            int pointsPerAxis = chunk.PointsPerAxis;
            int pointsVolume = pointsPerAxis * pointsPerAxis * pointsPerAxis;
            float[] pointsArray = new float[pointsVolume];
            pipeline.pointsBuffer.GetData(pointsArray);
            if (chunk is IMarchingCubeChunk c)
            {
                c.Points = pointsArray;
                storageGroup.Store(chunk.AnchorPos, chunk as IMarchingCubeChunk, true);
            }
        }

        public float[] GenerateAndGetNoiseForChunk(ICompressedMarchingCubeChunk chunk)
        {
            float[] result;
            int pointsPerAxis = chunk.PointsPerAxis;
            TryLoadOrGenerateNoise(chunk);
            result = new float[pointsPerAxis * pointsPerAxis * pointsPerAxis];
            pipeline.pointsBuffer.GetData(result, 0, 0, result.Length);
            return result;
        }


        public float[] RequestNoiseForChunk(ICompressedMarchingCubeChunk chunk)
        {
            float[] result;
            if (!storageGroup.TryLoadNoise(chunk.AnchorPos, chunk.ChunkSizePower, out result, out bool _))
            {
                result = GenerateAndGetNoiseForChunk(chunk);
            }
            return result;
        }

        public void TryLoadOrGenerateNoise(ICompressedMarchingCubeChunk chunk)
        {
            bool hasStoredData = false;
            bool isMipMapComplete = false;
            int sizePow = chunk.ChunkSizePower;
            float[] storedNoiseData = null;
            if (sizePow <= MarchingCubeChunkHandler.STORAGE_GROUP_SIZE_POWER)
            {
                hasStoredData = storageGroup.TryLoadNoise(chunk.AnchorPos, sizePow, out storedNoiseData, out isMipMapComplete);
                if (hasStoredData && (!isMipMapComplete))
                {
                    pipeline.savedPointsBuffer.SetData(storedNoiseData);
                }
            }
            if (isMipMapComplete)
            {
                pipeline.pointsBuffer.SetData(storedNoiseData);
            }
            else
            {
                DispatchNoiseForChunk(chunk, hasStoredData);
            }
        }

        protected void DispatchNoiseForChunk(ICompressedMarchingCubeChunk chunk, bool hasStoredData)
        {
            int groupsPerAxis = Mathf.CeilToInt(THREAD_GROUP_SIZE_PER_AXIS);
            pipeline.ApplyDensityPropertiesForChunk(chunk, hasStoredData);
            pipeline.densityGeneratorShader.Dispatch(0, groupsPerAxis, groupsPerAxis, groupsPerAxis);
        }

        public bool WorkOnNoiseMap(ICompressedMarchingCubeChunk chunk, Action a)
        {
            bool storeNoise = false;
            if (a != null)
            {
                if (!(chunk is IMarchingCubeChunk))
                {
                    throw new ArgumentException("Chunk has to be storeable to be able to store requested noise!");
                }
                a();
                storeNoise = true;
            }
            return storeNoise;
        }

    }
}