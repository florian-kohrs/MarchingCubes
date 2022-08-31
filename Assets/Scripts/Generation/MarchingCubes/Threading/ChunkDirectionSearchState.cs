using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MarchingCubes 
{

    public class ChunkDirectionSearchState
    {

        public ChunkDirectionSearchState(Direction d, Stack<int> childIndices, ChunkGroupTreeNode lastParent)
        {
            this.d = d;
            this.childIndices = childIndices;
            this.lastParent = lastParent;
        }


        protected Direction d;
        protected Stack<int> childIndices;
        protected ChunkGroupTreeNode lastParent;

        public bool ContinueFollowPathBuildingNodesToEmptyLeafPosition(out ChunkGroupTreeNode lastValidParent, out int lastChildIndex)
        {
            return lastParent.ContinueFollowPathBuildingNodesToEmptyLeafPosition(d, childIndices, out lastValidParent, out lastChildIndex);
        }

    }

}