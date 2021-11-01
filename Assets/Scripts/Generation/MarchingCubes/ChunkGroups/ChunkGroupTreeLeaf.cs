using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public class ChunkGroupTreeLeaf : BaseChunkGroupOrganizer
    {

        public ChunkGroupTreeLeaf(IChunkGroupParent parent, IMarchingCubeChunk chunk, int index, int[] relativeAnchorPoint, int[] anchorPoint, int sizePower)
        {
            childIndex = index;
            this.parent = parent;
            this.chunk = chunk;
            chunk.AnchorPos = new Vector3Int(anchorPoint[0], anchorPoint[1],anchorPoint[2]);
            groupRelativeAnchorPosition = relativeAnchorPoint;
            chunk.ChunkSizePower = sizePower;
            chunk.SetLeaf(this);
        }

        public IChunkGroupParent parent;
         
        protected int childIndex;

        public void SplitLeaf(IMarchingCubeChunkHandler chunkHandler)
        {
            parent.SplitChild(this, childIndex, chunk, chunkHandler);
        }

        public bool AllSiblingsAreLeafsWithSameTargetLod()
        {
            return parent.AreAllChildrenLeafs(chunk.TargetLODPower);
        }

        public override int SizePower => chunk.ChunkSizePower; 

        public IMarchingCubeChunk chunk;

        public bool IsEmpty => chunk != null;

        public int[] groupRelativeAnchorPosition;

        public override int[] GroupRelativeAnchorPosition => groupRelativeAnchorPosition;

        public IMarchingCubeChunk GetChunkAtLocal(Vector3Int pos)
        {
            return chunk;
        }

        public override IMarchingCubeChunk GetChunkAtLocalPosition(int[] pos)
        {
            return chunk;
        }

        public override void SetChunkAtLocalPosition(int[] pos, IMarchingCubeChunk chunk, bool allowOverride)
        {
            //throw new System.NotImplementedException
            //    ($"Overriding leafes is not supported. tried to apply lower size to existing leaf requested size:{chunk.ChunkSize} at node {this.chunk.AnchorPos} with size {this.chunk.ChunkSize} ");
        }

        public override bool TryGetChunkAtLocalPosition(int[] pos, out IMarchingCubeChunk chunk)
        {
            chunk = this.chunk;
            return chunk != null;
        }

        public override bool RemoveChunkAtLocalPosition(int[] pos)
        {
            chunk.ResetChunk();
            return true;
        }

        public override bool HasChunkAtLocalPosition(int[] pos)
        {
            return true;
        }

    }

}