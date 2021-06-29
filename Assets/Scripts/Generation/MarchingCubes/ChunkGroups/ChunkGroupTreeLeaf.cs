using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public class ChunkGroupTreeLeaf : IChunkGroupOrganizer
    {

        public ChunkGroupTreeLeaf(IMarchingCubeChunk chunk)
        {
            this.chunk = chunk;
        }

        public ChunkGroupTreeLeaf(IChunkBuilder chunkBuilder, Vector3Int anchorPoint, int size, int lodPower)
        {
            ChunkBuilder = chunkBuilder;
            GroupAnchorPosition = anchorPoint;
            Size = size;
        }

        protected IMarchingCubeChunk chunk;

        public IChunkBuilder ChunkBuilder { set; protected get; }

        public int Size { get; set; }

        public bool IsEmpty => true;

        public Vector3Int GroupAnchorPosition { get; protected set; }
        Vector3Int IChunkGroupOrganizer.GroupAnchorPosition { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public IMarchingCubeChunk GetChunkAtLocal(Vector3Int pos)
        {
            return chunk;
        }

        public IMarchingCubeChunk GetChunkAtLocalPosition(Vector3Int pos)
        {
            return chunk;
        }

        public IMarchingCubeChunk BuildChunkAtLocalPosition(Vector3Int pos, int size, int lodPower)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetChunkAtLocalPosition(Vector3Int pos, out IMarchingCubeChunk chunk)
        {
            throw new System.NotImplementedException();
        }

        public bool RemoveChunkAt(Vector3Int pos)
        {
            throw new System.NotImplementedException();
        }
    }

}