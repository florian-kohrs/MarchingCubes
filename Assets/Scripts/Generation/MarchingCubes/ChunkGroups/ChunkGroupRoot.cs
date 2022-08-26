using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupRoot : GenericTreeRoot<CompressedMarchingCubeChunk, ChunkGroupTreeLeaf, IChunkGroupOrganizer<CompressedMarchingCubeChunk>>, IChunkGroupParent<ChunkGroupTreeLeaf>
    {

        public ChunkGroupRoot(int[] coord, int chunkGroupSize) : base(coord, chunkGroupSize)
        {
        }

        public override int Size => MarchingCubeChunkHandler.CHUNK_GROUP_SIZE;

        public override int SizePower => MarchingCubeChunkHandler.CHUNK_GROUP_SIZE_POWER;
    
        public override IChunkGroupOrganizer<CompressedMarchingCubeChunk> GetLeaf(CompressedMarchingCubeChunk leaf, int index, int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeLeaf(this, leaf, index, anchor, relAnchor, sizePow);
        }

        public override IChunkGroupOrganizer<CompressedMarchingCubeChunk> GetNode(int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeNode(this, anchor, relAnchor, sizePow);
        }

        public void RemoveChildAtIndex(int index, CompressedMarchingCubeChunk chunk)
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

        public void PrepareBranchDestruction(List<CompressedMarchingCubeChunk> allLeafs)
        {
            throw new System.NotImplementedException();
        }

        public IChunkGroupParent<ChunkGroupTreeLeaf> AscendParentHirachy(int steps)
        {
            throw new System.NotImplementedException();
        }

        public int FindTargetLodThatWorksForHirachyOfAtLeast(int targetLodPower)
        {
            throw new System.NotImplementedException();
        }

    }
}