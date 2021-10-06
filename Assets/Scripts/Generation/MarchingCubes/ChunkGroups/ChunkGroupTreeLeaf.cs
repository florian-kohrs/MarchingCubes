using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public class ChunkGroupTreeLeaf : BaseChunkGroupOrganizer
    {

        public ChunkGroupTreeLeaf(IChunkGroupParent parent, IMarchingCubeChunk chunk, int index, int[] anchorPoint, int size)
        {
            this.childIndex = index;
            this.parent = parent;
            this.chunk = chunk;
            chunk.AnchorPos = new Vector3Int(anchorPoint[0], anchorPoint[1],anchorPoint[2]);
            chunk.ChunkSize = size;
            chunk.SetLeaf(this);
        }

        protected IChunkGroupParent parent;

        protected int childIndex;

        public void SplitLeaf(IMarchingCubeChunkHandler chunkHandler)
        {
            parent.SplitChild(this, childIndex, chunk, chunkHandler);
        }

        public override int Size => chunk.ChunkSize; 

        protected IMarchingCubeChunk chunk;

        public bool IsEmpty => chunk != null;

        public override int[] GroupRelativeAnchorPosition => default;

        public IMarchingCubeChunk GetChunkAtLocal(Vector3Int pos)
        {
            return chunk;
        }

        public override IMarchingCubeChunk GetChunkAtLocalPosition(int[] pos)
        {
            return chunk;
        }

        public override void SetChunkAtLocalPosition(int[] pos, IMarchingCubeChunk chunk)
        {
            throw new System.NotImplementedException
                ($"Overriding leafes is not supported. tried to apply lower size to existing leaf requested size:{chunk.ChunkSize} at node {GroupAnchorPosition} with size {Size} ");
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