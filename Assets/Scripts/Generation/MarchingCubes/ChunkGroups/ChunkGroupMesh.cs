using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupMesh : GroupMesh<ChunkGroupTreeNode, CompressedMarchingCubeChunk, ChunkGroupTreeLeaf, IChunkGroupDestroyableOrganizer<CompressedMarchingCubeChunk>>
    {

        public ChunkGroupMesh(int groupSize) : base(groupSize) { }


        protected override ChunkGroupTreeNode CreateKey(Vector3Int coord)
        {
            int[] chunkCoord = new int[] { coord.x, coord.y, coord.z };
            return new ChunkGroupTreeNode(null, chunkCoord, chunkCoord, 0, GROUP_SIZE_POWER);
        }


        public bool TryGetReadyChunkAt(int[] p, out CompressedMarchingCubeChunk chunk)
        {
            return TryGetGroupItemAt(p, out chunk) && chunk.IsReady;
        }

        public bool HasLeafAtGlobalPosition(int[] p)
        {
            return TryGetGroupItemAt(p, out _);
        }


    }
}