using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupMesh : GroupMesh<ChunkGroupTreeNode, CompressedMarchingCubeChunk>
    {

        public ChunkGroupMesh(int groupSize) : base(groupSize) { }


        protected override ChunkGroupTreeNode CreateKey(Vector3Int coord)
        {
            int[] chunkCoord = new int[] { coord.x, coord.y, coord.z };
            return new ChunkGroupTreeNode(this, chunkCoord, chunkCoord, 0, GROUP_SIZE_POWER);
        }

        public ChunkGroupTreeNode GetOrCreateNodeInDirection(ChunkGroupTreeNode node, Direction d)
        {
            int[] newChunkCoord = node.GroupAnchorPositionCopy;
            DirectionHelper.OffsetIntArray(d, newChunkCoord, GROUP_SIZE);
            return GetOrCreateGroupAtGlobalPosition(newChunkCoord);
        }

        public bool TryCreateEmptyChunkGroupAdjacentTo(CompressedMarchingCubeChunk chunk, Direction d)
        {
            if(TryGetGroupAt(chunk.Leaf.GroupAnchorPosition))
            if(TryGetNodeInDirection)
        }

        public bool TryGetNodeInDirection(ChunkGroupTreeNode node, Direction d, out ChunkGroupTreeNode neighbourNode)
        {
            int[] newChunkCoord = node.GroupAnchorPositionCopy;
            DirectionHelper.OffsetIntArray(d, newChunkCoord, GROUP_SIZE);
            return TryGetGroupAt(newChunkCoord, out neighbourNode);
        }


    }
}