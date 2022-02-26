using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkNeighourTask
    {
        public ChunkNeighourTask(CompressedMarchingCubeChunk chunk, MeshData meshData)
        {
            this.chunk = chunk;
            this.meshData = meshData;
        }

        public CompressedMarchingCubeChunk chunk;

        public MeshData meshData;

    }
}