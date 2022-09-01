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
            chunk.ChunkSizePower = sizePower;
            ///only register if the leaf can be split
            int halfSize = (int)Mathf.Pow(2, sizePower) / 2;
            centerPosition = new Vector3(anchorPoint[0] + halfSize, anchorPoint[1] + halfSize, anchorPoint[2] + halfSize);
            chunk.Leaf = this;

            if (!MarchingCubeChunkHandler.InitialWorldBuildingDone) Register();
        }

        protected Vector3 centerPosition;

        public Vector3 Center => centerPosition;

        protected bool RegisterInConstructor => false;

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
            if (leaf.LODPower > 0 && leaf.LODPower < MarchingCubeChunkHandler.DEACTIVATE_CHUNK_LOD_POWER)
            {
                isRegistered = true;
                ChunkUpdateRoutine.RegisterChunkLeaf(leaf.LODPower - 1, this);
            }
        }
        
        public void RemoveChildsFromRegister()
        {
            if (isRegistered)
            {
                ChunkUpdateRoutine.RemoveChunkLeaf(leaf.LODPower - 1,this);
                isRegistered = false;
            }
        }

    }

}