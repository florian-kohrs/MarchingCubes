using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupRoot : GenericTreeRoot<ICompressedMarchingCubeChunk, ChunkGroupTreeLeaf, IChunkGroupOrganizer<ICompressedMarchingCubeChunk>>, IChunkGroupParent<ChunkGroupTreeLeaf>
    {

        public ChunkGroupRoot(int[] coord) : base(coord, MarchingCubeChunkHandler.CHUNK_GROUP_SIZE)
        {
        }

        public override int Size => MarchingCubeChunkHandler.CHUNK_GROUP_SIZE;

        public override int SizePower => MarchingCubeChunkHandler.CHUNK_GROUP_SIZE_POWER;
        
        public void PrepareBranchDestruction(List<ICompressedMarchingCubeChunk> allLeafs)
        {
            throw new System.NotImplementedException();
        }

        public override IChunkGroupOrganizer<ICompressedMarchingCubeChunk> GetLeaf(ICompressedMarchingCubeChunk leaf, int index, int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeLeaf(this, leaf, index, anchor, relAnchor, sizePow);
        }

        public override IChunkGroupOrganizer<ICompressedMarchingCubeChunk> GetNode(int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeNode(anchor, relAnchor, sizePow);
        }

        public void RemoveChildAtIndex(int index, ICompressedMarchingCubeChunk chunk)
        {
            if(child.IsLeaf && child is ChunkGroupTreeLeaf leaf && leaf.leaf == chunk)
            {
                child = null;
            }
        }


        public bool EntireHirachyHasAtLeastTargetLod(int targetLodPower)
        {
            throw new System.NotImplementedException();
        }

    }
}