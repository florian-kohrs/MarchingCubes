using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkReadyState
    {

        public ChunkGroupTreeNode node;

        public int childIndex;

        public ChunkReadyState(ChunkGroupTreeNode node, int childIndex)
        {
            this.node = node;
            this.childIndex = childIndex;
        }
    }
}