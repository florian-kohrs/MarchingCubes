using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class NoisePipeline 
    {

        public const float THREAD_GROUP_SIZE_PER_AXIS = 4;

        public NoisePipeline(ChunkGenerationPipeline pipeline)
        {
            this.pipeline = pipeline;
        }

        protected ChunkGenerationPipeline pipeline;

        public float[] GenerateAndGetNoiseForChunk(ICompressedMarchingCubeChunk chunk)
        {
            float[] result;
            int pointsPerAxis = chunk.PointsPerAxis;
            TryLoadOrGenerateNoise(chunk);
            result = new float[pointsPerAxis * pointsPerAxis * pointsPerAxis];
            pipeline.pointsBuffer.GetData(result, 0, 0, result.Length);
            return result;
        }

        protected void TryLoadOrGenerateNoise(ICompressedMarchingCubeChunk chunk)
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
            pipeline.densityGeneratorShader.SetBool("tryLoadData", hasStoredData);
            pipeline.ApplyDensityPropertiesForChunk(chunk);
            pipeline.densityGeneratorShader.Dispatch(0, groupsPerAxis, groupsPerAxis, groupsPerAxis);
        }

    }
}