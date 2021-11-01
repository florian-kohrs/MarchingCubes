using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public class ChunkGroupTreeLeaf : GenericTreeLeaf<IMarchingCubeChunk, IChunkGroupParent<ChunkGroupTreeLeaf>>
    {

        public ChunkGroupTreeLeaf(IChunkGroupParent<ChunkGroupTreeLeaf> parent, IMarchingCubeChunk chunk, int index, int[] relativeAnchorPoint, int[] anchorPoint, int sizePower) 
            : base(parent,chunk,index,relativeAnchorPoint,anchorPoint,sizePower)
        {
            chunk.AnchorPos = new Vector3Int(anchorPoint[0], anchorPoint[1],anchorPoint[2]);
            chunk.ChunkSizePower = sizePower;
            chunk.SetLeaf(this);
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