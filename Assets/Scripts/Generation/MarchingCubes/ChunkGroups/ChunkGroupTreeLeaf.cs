using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public class ChunkGroupTreeLeaf : IChunkLocator
    {

        public ChunkGroupTreeLeaf(IMarchingCubeChunk chunk)
        {
            this.chunk = chunk;
        }

        protected IMarchingCubeChunk chunk;

        public IMarchingCubeChunk GetChunkAtLocal(Vector3Int pos)
        {
            return chunk;
        }
    }

}