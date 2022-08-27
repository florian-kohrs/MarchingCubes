using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MarchingCubes
{

    public class ChunkSplitExchange
    {

        public ChunkGroupTreeLeaf leaf;

        public List<ChunkGroupTreeNode> newNodes;

        public ChunkSplitExchange(ChunkGroupTreeLeaf leaf, List<ChunkGroupTreeNode> newNodes)
        {
            this.leaf = leaf;
            this.newNodes = newNodes;
        }
    }
}