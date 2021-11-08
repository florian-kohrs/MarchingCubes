using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public class ChunkGroupTreeLeaf : GenericTreeLeaf<IMarchingCubeChunk>
    {

        public ChunkGroupTreeLeaf(IChunkGroupParent<ChunkGroupTreeLeaf> parent, IMarchingCubeChunk chunk, int index, int[] relativeAnchorPoint, int[] anchorPoint, int sizePower) 
            : base(chunk,index,relativeAnchorPoint,anchorPoint,sizePower)
        {
            this.parent = parent;
            chunk.AnchorPos = new Vector3Int(anchorPoint[0], anchorPoint[1],anchorPoint[2]);
            chunk.ChunkSizePower = sizePower;
            chunk.SetLeaf(this);
        }


        public IChunkGroupParent<ChunkGroupTreeLeaf> parent;

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


        public void RemoveLeaf(IMarchingCubeChunk chunk)
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


        public bool AllSiblingsAreLeafsWithSameTargetLod()
        {
            return parent.AreAllChildrenLeafs(leaf.TargetLODPower);
        }

        public override bool RemoveLeafAtLocalPosition(int[] pos)
        {
            leaf.ResetChunk();
            return true;
        }
    }

}