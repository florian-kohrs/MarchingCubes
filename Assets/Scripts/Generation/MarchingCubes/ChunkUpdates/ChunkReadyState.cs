using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkReadyState
    {

        public ChunkGroupTreeNode lastParent;

        public int lastParentsChildIndex;

        public bool HasLeaf(out CompressedMarchingCubeChunk chunk)
        {
            var child = lastParent.children[lastParentsChildIndex];
            if (child is ChunkGroupTreeLeaf l)
            {
                chunk = l.leaf;
                return true;
            }
            else 
            {
                chunk = null;
                return false;
            }
        }

        public ChunkReadyState(ChunkGroupTreeNode node, int childIndex)
        {
            lastParent = node;
            lastParentsChildIndex = childIndex;
        }
    }
}