using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public class ChunkGroupTreeLeaf : GenericTreeLeaf<CompressedMarchingCubeChunk>, IChunkGroupDestroyableOrganizer<CompressedMarchingCubeChunk>
    {

        //~ChunkGroupTreeLeaf()
        //{
        //    Debug.Log("destroyed leaf");
        //}

        public ChunkGroupTreeLeaf(IChunkGroupParent<ChunkGroupTreeLeaf> parent, CompressedMarchingCubeChunk chunk, int index, int[] relativeAnchorPoint, int[] anchorPoint, int sizePower) 
            : base(chunk,index,relativeAnchorPoint,anchorPoint,sizePower)
        {
            this.parent = parent;
            chunk.AnchorPos = new Vector3Int(anchorPoint[0], anchorPoint[1],anchorPoint[2]);
            chunk.ChunkSizePower = sizePower;
            ///only register if the leaf can be split
            if (sizePower > MarchingCubeChunkHandler.MIN_CHUNK_SIZE_POWER)
            {
                isRegistered = true;
                ChunkUpdateRoutine.chunkGroupTreeLeaves.Add(this);
            }
            chunk.Leaf = this;
        }


        public IChunkGroupParent<ChunkGroupTreeLeaf> parent;

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


        public void RemoveLeaf(CompressedMarchingCubeChunk chunk)
        {
            parent.RemoveChildAtIndex(childIndex, chunk);
        }

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

        public override bool RemoveLeafAtLocalPosition(int[] pos)
        {
            DestroyBranch();
            return true;
        }

        public void DestroyBranch()
        {
            if(isRegistered)
            {
                ChunkUpdateRoutine.chunkGroupTreeLeaves.Remove(this);
                isRegistered = false;
            }
            leaf.DestroyChunk();
        }
    }

}