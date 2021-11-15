using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupTreeNode : GenericTreeNode<IMarchingCubeChunk, IChunkGroupOrganizer<IMarchingCubeChunk>, ChunkGroupTreeLeaf>, IChunkGroupParent<ChunkGroupTreeLeaf>
    {

        public ChunkGroupTreeNode(
            int[] anchorPosition,
            int[] relativeAnchorPosition, 
            int sizePower) : base(anchorPosition, relativeAnchorPosition, sizePower)
        {
        }

        public bool EntireHirachyHasAtLeastTargetLod(int targetLodPower)
        {
            bool result = true;
            for (int i = 0; i < 8 && result; i++)
            {
                result = children[i] == null 
                    || ((children[i] is ChunkGroupTreeLeaf l) 
                    && l.leaf.IsReady 
                    && l.leaf.TargetLODPower >= targetLodPower);
            }
            return result;
        }


        public void PrepareBranchDestruction(List<IMarchingCubeChunk> allLeafs)
        {
            for (int i = 0; i < 8 ; i++)
            {
                IChunkGroupOrganizer<IMarchingCubeChunk> child = children[i];
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

        public void RemoveChildAtIndex(int index, IMarchingCubeChunk chunk)
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

        public override IChunkGroupOrganizer<IMarchingCubeChunk> GetLeaf(IMarchingCubeChunk leaf, int index, int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeLeaf(this, leaf, index, anchor, relAnchor, sizePow);
        }

        public override IChunkGroupOrganizer<IMarchingCubeChunk> GetNode(int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeNode(anchor, relAnchor, sizePow);
        }

    
    }
}