using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public class ChunkGroupTreeLeaf : BaseChunkGroupOrganizer
    {

        public ChunkGroupTreeLeaf(IMarchingCubeChunk chunk, Vector3Int anchorPoint, int size)
        {
            this.chunk = chunk;
            chunk.AnchorPos = anchorPoint;
            chunk.ChunkSize = size;
        }


        public override int Size => chunk.ChunkSize; 

        protected IMarchingCubeChunk chunk;

        public bool IsEmpty => chunk != null;

        public override Vector3Int GroupRelativeAnchorPosition => default;

        public IMarchingCubeChunk GetChunkAtLocal(Vector3Int pos)
        {
            return chunk;
        }

        public override IMarchingCubeChunk GetChunkAtLocalPosition(Vector3Int pos)
        {
            return chunk;
        }

        public override void SetChunkAtLocalPosition(Vector3Int pos, IMarchingCubeChunk chunk)
        {
            throw new System.NotImplementedException
                ("Overriding leafes is not supported. tried to apply lower size to existing leaf");
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