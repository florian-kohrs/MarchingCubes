using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupRoot : GenericTreeRoot<IMarchingCubeChunk, ChunkGroupTreeLeaf, IChunkGroupOrganizer<IMarchingCubeChunk>>, IChunkGroupParent<ChunkGroupTreeLeaf>
    {

        public ChunkGroupRoot(int[] coord) : base(coord, MarchingCubeChunkHandler.CHUNK_GROUP_SIZE)
        {
        }

        public override int Size => MarchingCubeChunkHandler.CHUNK_GROUP_SIZE;

        public override int SizePower => MarchingCubeChunkHandler.CHUNK_GROUP_SIZE_POWER;

        public int[][] GetAllChildGlobalAnchorPosition()
        {
            if (child is ChunkGroupTreeNode node)
            {
                return node.GetAllChildGlobalAnchorPosition();
            }
            else
            {
                return ((ChunkGroupTreeNode)GetNode(GroupAnchorPosition, GroupAnchorPosition, SizePower)).GetAllChildGlobalAnchorPosition();
            }
        }

        public override IChunkGroupOrganizer<IMarchingCubeChunk> GetLeaf(IMarchingCubeChunk leaf, int index, int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeLeaf(this, leaf, index, anchor, relAnchor, sizePow);
        }

        public override IChunkGroupOrganizer<IMarchingCubeChunk> GetNode(int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeNode(anchor, relAnchor, sizePow);
        }

        public void SplitLeaf(int index)
        {
            throw new System.NotImplementedException();
        }
    }
}