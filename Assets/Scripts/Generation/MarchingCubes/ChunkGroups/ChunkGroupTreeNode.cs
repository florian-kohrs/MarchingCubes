using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupTreeNode : GenericTreeNode<CompressedMarchingCubeChunk, IChunkGroupDestroyableOrganizer<CompressedMarchingCubeChunk>, ChunkGroupTreeLeaf, ChunkGroupTreeNode>, IChunkGroupDestroyableOrganizer<CompressedMarchingCubeChunk>, IRegisterableNode
    {

        public ChunkGroupTreeNode(
            ChunkGroupTreeNode parent,
            int[] anchorPosition,
            int[] relativeAnchorPosition,
            int index,
            int sizePower) : base(parent, anchorPosition, relativeAnchorPosition, index, sizePower)
        {
            Initialize();
        }

        public ChunkGroupTreeNode(
            ChunkGroupMesh mesh,
            int[] anchorPosition,
            int[] relativeAnchorPosition,
            int index,
            int sizePower) : base(null, anchorPosition, relativeAnchorPosition, index, sizePower)
        {
            this.mesh = mesh;
            Initialize();
        }

        protected void Initialize()
        {
            centerPosition = new Vector3(GroupAnchorPosition[0] + halfSize, GroupAnchorPosition[1] + halfSize, GroupAnchorPosition[2] + halfSize);
            if (!MarchingCubeChunkHandler.InitialWorldBuildingDone) Register();
        }

        protected ChunkGroupMesh mesh;

        public bool ChanneledForDestruction { get; private set; }
       
        public bool ChanneledForDeactivation { get; private set; }

        public void SetChannelChunkForDeactivation()
        {
            ChanneledForDeactivation = true;
        }

        public void SetChannelChunkForDestruction()
        {
            ChanneledForDestruction = true;
        }

        protected const int CHILD_COUNT = 8;

        protected int RegisterIndex => LodPower - 1;
        
        public int LodPower => sizePower - MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE_POWER;

        protected Vector3 centerPosition;

        public Vector3 Center => centerPosition;

        protected override ChunkGroupTreeNode GetSelf => this;

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

        public Vector3 GetChildCenterPositionAtIndex(int index)
        {
            return Center + GetDirectionFromIndex(index) * halfSize;
        }


        public bool TryGetEmptyLeafParentInDirection(Direction d, Stack<int> childIndices, out ChunkGroupTreeNode parent)
        {
            int childIndex = childIndices.Pop();
            int depth = childIndices.Count;
            if (HasDirectionAvailable(d, children[childIndex].GroupRelativeAnchorPosition))
            {
                int newChildIndex = DirectionToNewChildIndex(d, childIndex, 1);
                parent = GetSelf;
                if (depth == 0)
                {
                    childIndices.Push(newChildIndex);
                    return !HasChildAtIndex(childIndex);
                }
                else
                {
                    var child = children[newChildIndex];
                    if (child is ChunkGroupTreeNode node)
                    {
                        return node.TryGetEmptyLeafParentInDirection(d, childIndices, out parent);
                    }
                    else
                    {
                        childIndices.Push(newChildIndex);
                        childIndices.Push(childIndex);
                        return child == null;
                    }
                }
            }
            else
            {
                if (IsRoot)
                {
                    if(mesh.TryGetNodeInDirection(this,d, out ChunkGroupTreeNode neighbour))
                    {
                        int oldSwappedChildIndex = DirectionToNewChildIndex(d, childIndex, -1);
                        childIndices.Push(oldSwappedChildIndex);
                        return neighbour.TryGetEmptyLeafParentInDirection(d,childIndices, out parent);
                    }
                    else
                    {
                        ///neighbour doesnt exist yet and since this runs
                        ///async it isnt allowed to create anything
                        ///continue on main thread
                        childIndices.Push(childIndex);
                        parent = GetSelf;
                        return true;
                    }
                }
                else
                {
                    int oldSwappedChildIndex = DirectionToNewChildIndex(d, childIndex, -1);
                    childIndices.Push(oldSwappedChildIndex);
                    childIndices.Push(index);
                    return Parent.TryGetEmptyLeafParentInDirection(d, childIndices, out parent);
                }
            }
        }

        public bool ContinueFollowPathBuildingNodesToEmptyLeafPosition(Direction d, Stack<int> childIndices, out ChunkGroupTreeNode lastValidParent, out int lastChildIndex)
        {
            int previousChildIndex = childIndices.Pop();
            if (HasDirectionAvailable(d, children[previousChildIndex].GroupRelativeAnchorPosition))
            {
                return FollowPathBuildingNodesToEmptyLeafPosition(d, childIndices, out lastValidParent, out lastChildIndex);
            }
            else
            {
                ChunkGroupTreeNode neighbour = mesh.GetOrCreateNodeInDirection(this, d);
                int oldSwappedChildIndex = DirectionToNewChildIndex(d, previousChildIndex, -1);
                childIndices.Push(oldSwappedChildIndex);
                return neighbour.FollowPathBuildingNodesToEmptyLeafPosition(d, childIndices, out lastValidParent, out lastChildIndex);
            }
        }

        protected bool FollowPathBuildingNodesToEmptyLeafPosition(Direction d, Stack<int> childIndices, out ChunkGroupTreeNode lastValidParent, out int nextChildIndex)
        {
            bool result;
            nextChildIndex = childIndices.Pop();
            bool shouldChildBeSplitAgain = !HasLeafAtIndex(nextChildIndex) && RegisterIndex > 0 && ChunkUpdateRoutine.HasLowerLodPowerAs(GetChildCenterPositionAtIndex(nextChildIndex), LodPower - 1);
            if (!shouldChildBeSplitAgain)
            {
                lastValidParent = this;
                result = !HasChildAtIndex(nextChildIndex);
            }
            else
            {
                if(childIndices.Count == 0)
                {
                    Debug.Log("Denied placement due to new childs position couldnt be determined." +
                        "Another child should be able to spawn this child tho.");
                    lastValidParent = null;
                    result = false;
                    ///doesnt have enough information to continue
                    ///there must(?) exist another neighbour of this chunk which has a lower lod 
                }
                else 
                {
                    if (children[nextChildIndex] == null)
                    {
                        GetAnchorPositionsForChildAtIndex(nextChildIndex, out int[] globalPos, out int[] localPos);
                        ChunkGroupTreeNode node = GetNode(nextChildIndex, globalPos, localPos, sizePower - 1);
                        children[nextChildIndex] = node;
                    }
                    return ((ChunkGroupTreeNode)children[nextChildIndex]).FollowPathBuildingNodesToEmptyLeafPosition(d, childIndices, out lastValidParent, out nextChildIndex);
                }
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
                    ((ChunkGroupTreeNode)child).PrepareBranchDestruction(allLeafs);
                }
                
            }
        }

        public override ChunkGroupTreeLeaf GetLeaf(CompressedMarchingCubeChunk leaf, int index, int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeLeaf(this, leaf, index, anchor, relAnchor, sizePow);
        }

        public override ChunkGroupTreeNode GetNode(int index, int[] anchor, int[] relAnchor, int sizePow)
        {
            return new ChunkGroupTreeNode(this, anchor, relAnchor, index, sizePow);
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
                GetAnchorPositionsForChildAtIndex(index, out int[] anchorPos, out int[] relativePosition);
                newNode = new ChunkGroupTreeNode(this, anchorPos, relativePosition, index, SizePower - 1);
            }
            else
            {
                newNode = new ChunkGroupTreeNode(this, oldLeaf.GroupAnchorPositionCopy, oldLeaf.GroupRelativeAnchorPosition, index, SizePower-1);
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
            if(IsRoot)
                ChunkUpdateRoutine.RegisterChunkRoot(this);
            else
                ChunkUpdateRoutine.RegisterChunkNode(RegisterIndex, this);
        }

    }
}