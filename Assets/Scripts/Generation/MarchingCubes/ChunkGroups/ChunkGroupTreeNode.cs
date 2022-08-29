using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupTreeNode : GenericTreeNode<CompressedMarchingCubeChunk, IChunkGroupDestroyableOrganizer<CompressedMarchingCubeChunk>, ChunkGroupTreeLeaf>, IChunkGroupParent<ChunkGroupTreeLeaf>, IChunkGroupDestroyableOrganizer<CompressedMarchingCubeChunk>, IRegisterableNode
    {

        public ChunkGroupTreeNode(
            int[] anchorPosition,
            int[] relativeAnchorPosition,
            int index,
            IChunkGroupOrganizer<CompressedMarchingCubeChunk> parent,
            int sizePower) : base(anchorPosition, relativeAnchorPosition, index, sizePower)
        {
            this.parent = parent;
            int sizeHalf = (int)Mathf.Pow(2, sizePower) / 2;
            centerPosition = new Vector3(anchorPosition[0] + sizeHalf, anchorPosition[1] + sizeHalf, anchorPosition[2] + sizeHalf);
            if (!MarchingCubeChunkHandler.InitialWorldBuildingDone) Register();
        }


        protected IChunkGroupOrganizer<CompressedMarchingCubeChunk> parent;

        public IChunkGroupOrganizer<CompressedMarchingCubeChunk> Parent => parent;

        protected const int CHILD_COUNT = 8;

        protected int RegisterIndex => LodPower - 1;
        protected int LodPower => sizePower - MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE_POWER;

        protected Vector3 centerPosition;

        public Vector3 Center => centerPosition;

        protected bool IsEmpty()
        {
            bool result = true;
            for (int i = 0; i < CHILD_COUNT && result; i++)
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
                for (int i = 0; i < CHILD_COUNT; i++)
                {
                    result[i] = ((ChunkGroupTreeLeaf)children[i]);
                }
                return result;
            }
        }

        public override IChunkGroupDestroyableOrganizer<CompressedMarchingCubeChunk> GetLeaf(CompressedMarchingCubeChunk leaf, int index, int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeLeaf(this, leaf, index, anchor, relAnchor, sizePow);
        }

        public override IChunkGroupDestroyableOrganizer<CompressedMarchingCubeChunk> GetNode(int index, int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeNode(anchor, relAnchor, index, this, sizePow);
        }

        public void DestroyBranch()
        {
            for (int i = 0; i < CHILD_COUNT; i++)
            {
                if (children[i] == null)
                    continue;
                children[i].DestroyBranch();
            }
        }

        /// <summary>
        /// Chunks are only deactivated out of range where normaly the root connects to a leaf directly.
        /// if it doesnt connect to a leaf the players position jumped and the node shouldnt be existing
        /// </summary>
        public void DeactivateBranch()
        {
            DestroyBranch();
        }

        public void RemoveChildsFromRegister()
        {
            ChunkUpdateRoutine.RemoveChunkNode(RegisterIndex, this);
            for (int i = 0; i < CHILD_COUNT; i++)
            {
                if(children[i] != null)
                    children[i].RemoveChildsFromRegister();
            }
        }

        /// <summary>
        /// spawn a new chunk for given lod for each null child element
        /// </summary>
        /// <param name="index"></param>
        /// <param name="newNodes"></param>
        /// <param name="oldLeaf"></param>
        public void SplitChildAtIndex(int index, out List<ChunkGroupTreeNode> newNodes)
        {
            newNodes = new List<ChunkGroupTreeNode>();
            SplitChildAtIndex(index, newNodes);
        }


        protected void SplitChildAtIndex(int index, List<ChunkGroupTreeNode> newNodes)
        {
            ChunkGroupTreeNode newNode;
            var oldLeaf = children[index];
            if (oldLeaf == null)
            {
                int[] relativePosition = GetLocalPositionFromIndex(index);
                int[] anchorPos = new int[] {
                relativePosition[0] + GroupAnchorPosition [0],
                relativePosition[1] + GroupAnchorPosition[1],
                relativePosition[2] + GroupAnchorPosition[2]
                };
                newNode = new ChunkGroupTreeNode(anchorPos, relativePosition, index, this, SizePower - 1);
            }
            else
            {
                newNode = new ChunkGroupTreeNode(oldLeaf.GroupAnchorPositionCopy, oldLeaf.GroupRelativeAnchorPosition, index, this, SizePower-1);
            }
            children[index] = newNode;
            newNodes.Add(newNode);
            if (newNode.RegisterIndex > 0)
                newNode.CheckAnyChildrenForSplit(newNodes);
        }

        public void CheckAnyChildrenForSplit(List<ChunkGroupTreeNode> newNodes)
        {
            for (int i = 0;i < CHILD_COUNT; i++)
            {
                //TODO: Improve performance here (maybe!)
                Vector3 position = GetChildCenterPositionForIndex(i);
                int lodPower = ChunkUpdateRoutine.GetLodPowerForPosition(position);
                if (lodPower < RegisterIndex)
                    SplitChildAtIndex(index, newNodes);
            }
        }

        public void Register()
        {
            ChunkUpdateRoutine.RegisterChunkNode(RegisterIndex, this);
        }

    }
}