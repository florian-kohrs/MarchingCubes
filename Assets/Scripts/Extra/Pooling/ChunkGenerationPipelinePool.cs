using MarchingCubes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerationPipelinePool : DisposablePoolOf<ChunkGenerationPipeline>
{

    public ChunkGenerationPipelinePool(Func<ChunkGenerationPipeline> CreateItem) : base(CreateItem)
    {
    }

    protected override ChunkGenerationPipeline BuildItemInstance()
    {
        ChunkGenerationPipeline result = base.BuildItemInstance();
        result.ApplyStaticProperties();
        return result;
    }

    public ChunkGenerationPipeline GetChunkGenerationPipelineFor(ICompressedMarchingCubeChunk chunk)
    {
        ChunkGenerationPipeline chunkGenerationPipeline = GetItemFromPool();

        chunkGenerationPipeline.ApplyChunkDataToShaders(chunk);

        return chunkGenerationPipeline;
    }


}
