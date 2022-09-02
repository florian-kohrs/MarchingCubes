using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MarchingCubes 
{

    public class ChunkDirectionSearchState
    {

        public ChunkDirectionSearchState(Direction d)
        {
            direction = d;
            childIndices = new Stack<int>();
        }


        public Direction direction;

        public Stack<int> childIndices;
        public ChunkGroupTreeNode lastParent;
        public bool isInDownWardsTrend;
        public int lastChildIndex;

        public bool ContinueFollowPathBuildingNodesToEmptyLeafPosition(out ChunkReadyState readyState)
        {
            bool result = lastParent.ContinueFollowPathBuildingNodesToEmptyLeafPosition(this);
            readyState = GetReadyState();
            return result;
        }

        public ChunkReadyState GetReadyState()
        {
            return new ChunkReadyState(lastParent, lastChildIndex);
        }

    }

}