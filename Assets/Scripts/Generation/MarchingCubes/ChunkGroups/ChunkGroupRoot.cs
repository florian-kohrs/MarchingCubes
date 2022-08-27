using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupRoot : GenericTreeRoot<CompressedMarchingCubeChunk, ChunkGroupTreeLeaf, IChunkGroupDestroyableOrganizer<CompressedMarchingCubeChunk>>, IChunkGroupParent<ChunkGroupTreeLeaf>
    {

        public ChunkGroupRoot(int[] coord, int chunkGroupSize) : base(coord, chunkGroupSize)
        {
            ChunkUpdateRoutine.chunkGroupTreeRoots.Add(this);
            float halfSize = chunkGroupSize / 2;
            center = new Vector3(coord[0] + halfSize, coord[1] + halfSize, coord[2] + halfSize);
        }

        protected Vector3 center;

        protected bool channeledForDestruction;
        protected bool channeledForDeactivation;

        public bool ChanneledForDestruction { 
            get { return channeledForDestruction; }
        }

        public void SetChannelChunkForDestruction ()
        {
            channeledForDestruction = true;
            ChunkUpdateRoutine.chunkGroupTreeRoots.Remove(this);
            RemoveChildsFromRegister();
        }

        public bool ChanneledForDeactivation
        {
            get { return channeledForDeactivation; }
        }

        public void SetChannelChunkForDeactivation()
        {
            channeledForDeactivation = true;
            RemoveChildsFromRegister();
        }


        protected void RemoveChildsFromRegister()
        {
            child.RemoveChildsFromRegister();
        }


        public Vector3 Center => center;

        public override int Size => MarchingCubeChunkHandler.CHUNK_GROUP_SIZE;

        public override int SizePower => MarchingCubeChunkHandler.CHUNK_GROUP_SIZE_POWER;
    
        public override IChunkGroupDestroyableOrganizer<CompressedMarchingCubeChunk> GetLeaf(CompressedMarchingCubeChunk leaf, int index, int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeLeaf(this, leaf, index, anchor, relAnchor, sizePow);
        }

        public override IChunkGroupDestroyableOrganizer<CompressedMarchingCubeChunk> GetNode(int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeNode(anchor, relAnchor, 0, sizePow);
        }

        public void RemoveChildAtIndex(int index, CompressedMarchingCubeChunk chunk)
        {
            if(child.IsLeaf && child is ChunkGroupTreeLeaf leaf && leaf.leaf == chunk)
            {
                child = null;
            }
        }

        public void DestroyBranch()
        {
            child.DestroyBranch();
        }

        public void DeactivateBranch()
        {
            child.DeactivateBranch();
        }

        //exception is still valid here
        public void PrepareBranchDestruction(List<CompressedMarchingCubeChunk> allLeafs)
        {
            throw new System.NotImplementedException();
        }

        public void SplitChildAtIndex(int index, out List<ChunkGroupTreeNode> newNodes)
        {
            newNodes = new List<ChunkGroupTreeNode>();
            child = GetNode(GroupAnchorPosition, GroupAnchorPosition, SizePower);
            if(child is ChunkGroupTreeNode n)
            {
                newNodes.Add(n);
                n.CheckAnyChildrenForSplit(newNodes);
            }
            else
            {
                throw new System.Exception();
            }
        }
    }
}