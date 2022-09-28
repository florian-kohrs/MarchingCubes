using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupMesh : GroupMesh<ChunkGroupTreeNode, CompressedMarchingCubeChunk>
    {

        public ChunkGroupMesh(int groupSize) : base(groupSize) { }


        protected override ChunkGroupTreeNode CreateRootNodeAt(Vector3Int coord)
        {
            int[] chunkCoord = new int[] { coord.x, coord.y, coord.z };
            return new ChunkGroupTreeNode(this, chunkCoord, chunkCoord, 0, GROUP_SIZE_POWER, true);
        }

        public ChunkGroupTreeNode CreateInactiveRootNodeAt(int[] globalPos)
        {
            Vector3Int coord = PositionToGroupCoord(globalPos);
            int[] chunkCoord = new int[] { globalPos[0], globalPos[1], globalPos[2] };
            ChunkGroupTreeNode result = new ChunkGroupTreeNode(this, chunkCoord, chunkCoord, 0, GROUP_SIZE_POWER, false);
            StoreGroupAtCoordinate(result, coord);
            return result;
        }

        public ChunkGroupTreeNode GetOrCreateRootNodeInDirection(ChunkGroupTreeNode node, Direction d)
        {
            int[] newChunkCoord = node.GroupAnchorPositionCopy;
            DirectionHelper.OffsetIntArray(d, newChunkCoord, GROUP_SIZE);
            return GetOrCreateGroupAtGlobalPosition(newChunkCoord);
        }

        public bool CreateInactiveRootNodeInDirection(ChunkGroupTreeNode node, Direction d, out ChunkGroupTreeNode result)
        {
            int[] newChunkPos = node.GroupAnchorPositionCopy;
            DirectionHelper.OffsetIntArray(d, newChunkPos, GROUP_SIZE);
            if (!HasGroupAtPos(newChunkPos))
            {
                result = CreateInactiveRootNodeAt(newChunkPos);
                return true;
            }
            result = null;
            return false;
        }

        public bool CreateEmptyChunkGroupAdjacentTo(CompressedMarchingCubeChunk chunk, Direction d)
        {
            return CreateEmptyChunkGroupAdjacentTo(chunk, d, out _);
        }

        public bool CreateEmptyChunkGroupAdjacentTo(CompressedMarchingCubeChunk chunk, Direction d, out ChunkGroupTreeNode result)
        {
            int[] chunkPos = chunk.Leaf.GroupAnchorPosition;
            return CreateEmptyChunkGroupAdjacentTo(GetGroupAt(chunkPos), d, out result);
        }

        public bool CreateEmptyChunkGroupAdjacentTo(ChunkGroupTreeNode root, Direction d, out ChunkGroupTreeNode result)
        {
            if (CreateInactiveRootNodeInDirection(root, d, out result))
            {
                return true;
            }
            return false;
        }

        public bool TryGetRootNodeInDirection(ChunkGroupTreeNode node, Direction d, out ChunkGroupTreeNode neighbourNode)
        {
            int[] newChunkCoord = node.GroupAnchorPositionCopy;
            DirectionHelper.OffsetIntArray(d, newChunkCoord, GROUP_SIZE);
            return TryGetGroupAt(newChunkCoord, out neighbourNode);
        }


    }
}