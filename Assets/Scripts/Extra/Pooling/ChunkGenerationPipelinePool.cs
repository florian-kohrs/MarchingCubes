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

    protected override void OnDispose()
    {
        ChunkGPUDataRequest.emptyMinDegreeBuffer.Dispose();
    }

}
