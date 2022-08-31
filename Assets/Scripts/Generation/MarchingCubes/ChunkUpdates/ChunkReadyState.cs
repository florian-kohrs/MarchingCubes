using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkReadyState
    {

        public ChunkGroupTreeNode lastParent;

        public int lastParentsChildIndex;

        public ChunkReadyState(ChunkGroupTreeNode node, int childIndex)
        {
            lastParent = node;
            lastParentsChildIndex = childIndex;
        }
    }
}