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
            int sizePower,
            bool isActive) : base(null, anchorPosition, relativeAnchorPosition, index, sizePower)
        {
            ChanneledForDeactivation = !isActive;
            this.mesh = mesh;
            Initialize();
        }

        protected void Initialize()
        {
            centerPosition = new Vector3(GroupAnchorPosition[0] + halfSize, GroupAnchorPosition[1] + halfSize, GroupAnchorPosition[2] + halfSize);
            if (IsRoot || !MarchingCubeChunkHandler.InitialWorldBuildingDone) Register();
        }

        protected ChunkGroupMesh mesh;

        public bool ChanneledForDestruction { get; private set; }
       
        public bool ChanneledForDeactivation { get; private set; }

        public void SetChannelChunkForDeactivation()
        {
            ChanneledForDeactivation = true;
            ChunkUpdateRoutine.MoveRootToDeactivation(this);
            RemoveChildsFromRegister();
        }

        public void SetChannelChunkForReactivation()
        {
            ChanneledForDeactivation = false;
            ChunkUpdateRoutine.MoveRootToActivation(this);
        }

        public void SetChannelChunkForDestruction()
        {
            ChanneledForDestruction = true;
            if(ChanneledForDeactivation)
                ChunkUpdateRoutine.RemoveInactiveChunkRoot(this);
            else
                ChunkUpdateRoutine.RemoveActiveChunkRoot(this);
            RemoveChildsFromRegister();
        }

        protected const int CHILD_COUNT = 8;

        public int RegisterIndex => LodPower - 1;
        
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

        public bool TryGetEmptyLeafParentInDirection(ChunkDirectionSearchState searchState, bool allowedToBuildNodes = false)
        {
            Direction d = searchState.direction;
            Stack<int> childIndices = searchState.childIndices;
            int childIndex = childIndices.Pop();
            int newChildIndex;
            if (HasDirectionAvailable(d, children[childIndex].GroupRelativeAnchorPosition))
            {
                searchState.isInDownWardsTrend = true;
                childIndices.Push(DirectionToNewChildIndex(d, childIndex, 1));
                bool canBuild = FollowPathBuildingNodesToEmptyLeafPosition(searchState, allowedToBuildNodes);
                childIndices.Push(searchState.lastChildIndex);
                return canBuild;
            }
            else
            {
                if (IsRoot)
                {
                    newChildIndex = DirectionToNewChildIndex(d, childIndex, -1);
                    childIndices.Push(newChildIndex);
                    ChunkGroupTreeNode neighbour = null;
                    if (allowedToBuildNodes)
                        neighbour = mesh.GetOrCreateRootNodeInDirection(this, d);
                    
                    if (neighbour != null || mesh.TryGetRootNodeInDirection(this,d, out neighbour))
                    {
                        bool result = neighbour.FollowPathBuildingNodesToEmptyLeafPosition(searchState, allowedToBuildNodes);
                        childIndices.Push(searchState.lastChildIndex);
                        searchState.isInDownWardsTrend = true;
                        return result;
                    }
                    else
                    {
                        ///neighbour doesnt exist yet and since this runs
                        ///async it isnt allowed to create anything
                        ///continue on main thread
                        searchState.lastParent = GetSelf;
                        return true;
                    }
                }
                else
                {
                    int oldSwappedChildIndex = DirectionToNewChildIndex(d, childIndex, -1);
                    childIndices.Push(oldSwappedChildIndex);
                    childIndices.Push(index);
                    return Parent.TryGetEmptyLeafParentInDirection(searchState);
                }
            }
        }

        public bool ContinueFollowPathBuildingNodesToEmptyLeafPosition(ChunkDirectionSearchState searchState)
        {
            if(searchState.isInDownWardsTrend)
            {
                return FollowPathBuildingNodesToEmptyLeafPosition(searchState, true);
            }
            else
            {
                ChunkGroupTreeNode neighbour = mesh.GetOrCreateRootNodeInDirection(this, searchState.direction);
                return neighbour.FollowPathBuildingNodesToEmptyLeafPosition(searchState, true);
            }
        }

        protected bool FollowPathBuildingNodesToEmptyLeafPosition(ChunkDirectionSearchState searchState, bool allowedToBuildNodes)
        {
            bool result;
            int nextChildIndex = searchState.childIndices.Pop();
            searchState.lastChildIndex = nextChildIndex;
            bool shouldChildBeSplitAgain = !HasLeafAtIndex(nextChildIndex) && RegisterIndex > 0 && ChunkUpdateRoutine.HasLowerLodPowerAs(GetChildCenterPositionAtIndex(nextChildIndex), LodPower - 1);
            if (!shouldChildBeSplitAgain)
            {
                searchState.lastParent = this;
                result = !HasChildAtIndex(nextChildIndex);
            }
            else
            {
                if(searchState.childIndices.Count == 0)
                {
                    ///cant continue search without child index
                    result = false;
                }
                else 
                {
                    var child = children[nextChildIndex];
                    ChunkGroupTreeNode node;
                    if (child == null)
                    {
                        if(allowedToBuildNodes)
                        {
                            GetAnchorPositionsForChildAtIndex(nextChildIndex, out int[] globalPos, out int[] localPos);
                            node = GetNode(nextChildIndex, globalPos, localPos, ChildrenSizePower);
                            SetNewChildAt(node, nextChildIndex);
                        }
                        else
                        {
                            searchState.lastParent = GetSelf;
                            return true;
                        }
                    }
                    else
                    {
                        node = child as ChunkGroupTreeNode;
                    }
                    return node.FollowPathBuildingNodesToEmptyLeafPosition(searchState, allowedToBuildNodes);
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
            DeactivateBranch();
        }

        protected void DestroyChildBranches()
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
            DestroyChildBranches();
            children = new IChunkGroupDestroyableOrganizer<CompressedMarchingCubeChunk>[CHILD_COUNT];
        }

        public void RemoveChildsFromRegister()
        {
            if(!IsRoot)
                ChunkUpdateRoutine.RemoveChunkNode(RegisterIndex, this);
            for (int i = 0; i < CHILD_COUNT; i++)
            {
                if(children[i] != null)
                    children[i].RemoveChildsFromRegister();
            }
        }

        public void AddChildsToRegister()
        {
            if (!IsRoot)
                Register();
            for (int i = 0; i < CHILD_COUNT; i++)
            {
                if (children[i] != null)
                    children[i].AddChildsToRegister();
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
                ChildCount++;
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

        public void BuildChildIndexList(int depth, Stack<int> stack)
        {
            if (depth <= 0)
                return;

            stack.Push(index);
            Parent.BuildChildIndexList(depth - 1, stack);
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
                if(ChanneledForDeactivation)
                    ChunkUpdateRoutine.RegisterDeactiveRoot(this);
                else
                    ChunkUpdateRoutine.RegisterActiveChunkRoot(this);
            else
                ChunkUpdateRoutine.RegisterChunkNode(RegisterIndex, this);
        }

    }
}