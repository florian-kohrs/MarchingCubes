using MarchingCubes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerationPipelinePool : DisposablePoolOf<ChunkGenerationGPUData>
{

    public ChunkGenerationPipelinePool(Func<ChunkGenerationGPUData> CreateItem) : base(CreateItem)
    {
    }

    public StorageGroupMesh storageGroup;

    protected override ChunkGenerationGPUData BuildItemInstance()
    {
        ChunkGenerationGPUData result = base.BuildItemInstance();
        result.ApplyStaticProperties();
        return result;
    }

    public ChunkGenerationGPUData GetChunkGenerationPipelineFor(ICompressedMarchingCubeChunk chunk)
    {
        ChunkGenerationGPUData chunkGenerationPipeline = GetItemFromPool();

        chunkGenerationPipeline.ApplyChunkDataToShaders(chunk);

        return chunkGenerationPipeline;
    }



}
