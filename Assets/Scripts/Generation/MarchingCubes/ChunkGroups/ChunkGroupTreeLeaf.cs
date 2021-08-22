using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public class ChunkGroupTreeLeaf : BaseChunkGroupOrganizer
    {

        public ChunkGroupTreeLeaf(IMarchingCubeChunk chunk)
        {
            this.chunk = chunk;
        }

        public ChunkGroupTreeLeaf(IChunkBuilder chunkBuilder, Vector3Int anchorPoint, Vector3Int relativeAnchorPoint, int size, int lodPower)
        {
            this.size = size;
            ChunkBuilder = chunkBuilder;
            GroupAnchorPosition = anchorPoint;
            groupRelativeAnchorPosition = relativeAnchorPoint; 
        }

        int size;

        public override int Size => size; 

        protected IMarchingCubeChunk chunk;

        Vector3Int groupRelativeAnchorPosition;

        public bool IsEmpty => chunk != null;

        public override Vector3Int GroupRelativeAnchorPosition => groupRelativeAnchorPosition;

        public IMarchingCubeChunk GetChunkAtLocal(Vector3Int pos)
        {
            return chunk;
        }

        public override IMarchingCubeChunk GetChunkAtLocalPosition(Vector3Int pos)
        {
            return chunk;
        }

        public override void SetChunkAtLocalPosition(Vector3Int pos, int size, int lodPower, IMarchingCubeChunk chunk)
        {
            chunk.AnchorPos = GroupAnchorPosition;
            chunk.ChunkSize = Size;
            this.chunk = chunk; 
        }


        public override bool TryGetChunkAtLocalPosition(Vector3Int pos, out IMarchingCubeChunk chunk)
        {
            chunk = this.chunk;
            return chunk != null;
        }

        public override bool RemoveChunkAtLocalPosition(Vector3Int pos)
        {
            chunk.ResetChunk();
            return true;
        }

        public override bool HasChunkAtLocalPosition(Vector3Int pos)
        {
            return true;
        }

    }

}