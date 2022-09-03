using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public class ChunkGroupTreeLeaf : GenericTreeLeaf<CompressedMarchingCubeChunk, IChunkGroupDestroyableOrganizer<CompressedMarchingCubeChunk>, ChunkGroupTreeLeaf, ChunkGroupTreeNode>, IChunkGroupDestroyableOrganizer<CompressedMarchingCubeChunk>
    {

        //~ChunkGroupTreeLeaf()
        //{
        //    Debug.Log("destroyed leaf");
        //}

        public ChunkGroupTreeLeaf(ChunkGroupTreeNode parent, CompressedMarchingCubeChunk chunk, int index, int[] anchorPoint, int[] relativeAnchorPoint, int sizePower) 
            : base(parent, chunk,index, anchorPoint, relativeAnchorPoint,sizePower)
        {
            chunk.AnchorPos = new Vector3Int(anchorPoint[0], anchorPoint[1],anchorPoint[2]);
            chunk.NodeSizePower = sizePower;
            ///only register if the leaf can be split
            int halfSize = (int)Mathf.Pow(2, sizePower) / 2;
            centerPosition = new Vector3(anchorPoint[0] + halfSize, anchorPoint[1] + halfSize, anchorPoint[2] + halfSize);
            chunk.Leaf = this;

            if (!MarchingCubeChunkHandler.InitialWorldBuildingDone) Register();
        }

        protected Vector3 centerPosition;

        public Vector3 Center => centerPosition;

        protected bool isRegistered;

        public int[][] GetAllChildGlobalAnchorPosition()
        {
            int halfSize = leaf.ChunkSize / 2;

            int[][] result = new int[8][];
            for (int i = 0; i < 8; i++)
            {
                result[i] = GetGlobalAnchorPositionForIndex(i, halfSize);
            }
            return result;
        }

        protected int RegisterIndex => SizePower - MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE_POWER - 1;

        protected int[] GetGlobalAnchorPositionForIndex(int index, int halfSize)
        {
            int[] result = { GroupAnchorPosition[0], GroupAnchorPosition[1], GroupAnchorPosition[2] };
            if (index == 1 || index == 3 || index == 5 || index == 6)
            {
                result[0] += halfSize;
            }
            if (index == 2 || index == 3 || index > 5)
            {
                result[1] += halfSize;
            }
            if (index >= 4)
            {
                result[2] += halfSize;
            }
            return result;
        }

        public Stack<int> BuildChildIndexList(int depth)
        {
            Stack<int> result = new Stack<int>();
            if (depth > 0)
            {
                result.Push(childIndex);
                parent.BuildChildIndexList(depth - 1, result);
            }
            return result;
        }

        public void DestroyLeaf()
        {
            leaf = default;
            RemoveChildsFromRegister();
            parent.RemoveChildAtIndex(childIndex);
        }

        public bool TryGetEmptyLeafParentInDirection(Direction d, out ChunkDirectionSearchState searchState)
        {
            searchState = new ChunkDirectionSearchState(d);
            searchState.childIndices.Push(childIndex);
            return parent.TryGetEmptyLeafParentInDirection(searchState);
        }

        public bool TryGetChunkInDirection(Direction d, out ChunkReadyState readyState)
        {
            ChunkDirectionSearchState searchState = new ChunkDirectionSearchState(d);
            searchState.childIndices.Push(childIndex);
            bool result = parent.TryGetEmptyLeafParentInDirection(searchState, true);
            result = result && searchState.lastParent.RegisterIndex == 0;
            if (result)
                readyState = searchState.GetReadyState();
            else
                readyState = null;
            return result;
        }


        public override bool RemoveLeafAtLocalPosition(int[] pos)
        {
            DestroyBranch();
            return true;
        }

        public void SplitChildAtIndex(out List<ChunkGroupTreeNode> newNodes)
        {
            parent.SplitChildAtIndex(childIndex, out newNodes);
        }

        public void DestroyBranch()
        {
            RemoveChildsFromRegister();
            leaf.DestroyChunk();
        }

        public void DeactivateBranch()
        {
            leaf.ResetChunk();
        }


        public void Register()
        {
            if (RegisterIndex >= 0 && RegisterIndex < MarchingCubeChunkHandler.MAX_CHUNK_LOD_POWER)
            {
                isRegistered = true;
                ChunkUpdateRoutine.RegisterChunkLeaf(RegisterIndex, this);
            }
        }
        
        public void RemoveChildsFromRegister()
        {
            if (isRegistered)
            {
                ChunkUpdateRoutine.RemoveChunkLeaf(RegisterIndex, this);
                isRegistered = false;
            }
        }

        public void AddChildsToRegister()
        {
            if (!isRegistered)
            {
                Register();
            }
        }

        protected override void SetValue(CompressedMarchingCubeChunk value)
        {
            if (value != null)
                throw new System.Exception("Chunk leafs are not allowed to override their value!");
            leaf = value;
        }
    }

}