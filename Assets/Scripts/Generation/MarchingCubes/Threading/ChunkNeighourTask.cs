using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkNeighourTask
    {
        public ChunkNeighourTask(ICompressedMarchingCubeChunk chunk, MeshData meshData)
        {
            this.chunk = chunk;
            this.meshData = meshData;
        }

        public ICompressedMarchingCubeChunk chunk;

        public MeshData meshData;

    }
}