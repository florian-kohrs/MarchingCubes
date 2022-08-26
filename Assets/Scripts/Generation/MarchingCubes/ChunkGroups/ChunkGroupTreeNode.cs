using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupTreeNode : GenericTreeNode<CompressedMarchingCubeChunk, IChunkGroupOrganizer<CompressedMarchingCubeChunk>, ChunkGroupTreeLeaf>, IChunkGroupParent<ChunkGroupTreeLeaf>
    {

        public ChunkGroupTreeNode(
            IChunkGroupParent<ChunkGroupTreeLeaf> parent,
            int[] anchorPosition,
            int[] relativeAnchorPosition, 
            int sizePower) : base(anchorPosition, relativeAnchorPosition, sizePower)
        {
            this.parent = parent;
        }

        protected IChunkGroupParent<ChunkGroupTreeLeaf> parent;

        public bool EntireHirachyHasAtLeastTargetLod(int targetLodPower)
        {
            bool result = true;
            for (int i = 0; i < 8 && result; i++)
            {
                result = children[i] == null 
                    || ((children[i] is ChunkGroupTreeLeaf l) 
                    && l.leaf.IsReady 
                    && l.leaf.TargetLODPower >= targetLodPower)
                    || ((children[i] is ChunkGroupTreeNode n)
                    && n.EntireHirachyHasAtLeastTargetLod(targetLodPower));
            }
            return result;
        }


        public int FindTargetLodThatWorksForHirachyOfAtLeast(int targetLodPower, int parentSteps, out IChunkGroupParent<ChunkGroupTreeLeaf> parentOfHirachy)
        {
            throw new NotImplementedException();
        }

        protected bool IsEmpty()
        {
            bool result = true;
            for (int i = 0; i < 8 && result; i++)
            {
                result = children[i] == null
                    || ((children[i] is ChunkGroupTreeNode n)
                    && n.IsEmpty());
            }
            return result;
        }

        public void PrepareBranchDestruction(List<CompressedMarchingCubeChunk> allLeafs)
        {
            for (int i = 0; i < 8 ; i++)
            {
                IChunkGroupOrganizer<CompressedMarchingCubeChunk> child = children[i];
                if (child == null)
                    continue;

                if (child is ChunkGroupTreeLeaf l)
                {
                    l.leaf.PrepareDestruction();
                    allLeafs.Add(l.leaf);
                }
                else
                {
                    ((IChunkGroupParent<ChunkGroupTreeLeaf>)child).PrepareBranchDestruction(allLeafs);
                }
                
            }
        }

        public void RemoveChildAtIndex(int index, CompressedMarchingCubeChunk chunk)
        {
            if(children[index] != null 
                && children[index].IsLeaf 
                && children[index] is ChunkGroupTreeLeaf leaf 
                && leaf.leaf == chunk)
            {
                children[index] = null;
            }
        }


        public override ChunkGroupTreeLeaf[] GetLeafs()
        {
            //if(AreAllChildrenLeafs())
            {
                ChunkGroupTreeLeaf[] result = new ChunkGroupTreeLeaf[8];
                for (int i = 0; i < 8; i++)
                {
                    result[i] = ((ChunkGroupTreeLeaf)children[i]);
                }
                return result;
            }
        }

        public override IChunkGroupOrganizer<CompressedMarchingCubeChunk> GetLeaf(CompressedMarchingCubeChunk leaf, int index, int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeLeaf(this, leaf, index, anchor, relAnchor, sizePow);
        }

        public override IChunkGroupOrganizer<CompressedMarchingCubeChunk> GetNode(int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeNode(this, anchor, relAnchor, sizePow);
        }

        public IChunkGroupParent<ChunkGroupTreeLeaf> AscendParentHirachy(int steps)
        {
            if (steps <= 0) return this;
            else return parent.AscendParentHirachy(steps - 1);
        }

    }
}