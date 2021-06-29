using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupRoot : IChunkGroupRoot
    {

        public IChunkBuilder ChunkBuilder { protected get;  set; }

        protected IChunkGroupOrganizer child;

        public int Size => MarchingCubeChunkHandler.CHUNK_GROUP_SIZE;

        public Vector3Int GroupAnchorPosition { get; set; }

        public IMarchingCubeChunk GetChunkAtLocalPosition(Vector3Int pos)
        {
            return child.GetChunkAtLocalPosition(pos);
        }

        public bool TryGetChunkAtLocalPosition(Vector3Int pos, out IMarchingCubeChunk chunk) => child.TryGetChunkAtLocalPosition(pos, out chunk);

        public IMarchingCubeChunk BuildChunkAtLocalPosition(Vector3Int globalPosition, int size, int lodPower)
        {
            if (!HasChild)
            {
                if(size == Size)
                {
                    child = new ChunkGroupTreeLeaf(ChunkBuilder, GroupAnchorPosition, size, lodPower);
                }
                else
                {
                    child = new ChunkGroupTreeNode(ChunkBuilder, GroupAnchorPosition, size);
                }
            }
            return child.BuildChunkAtLocalPosition(globalPosition, size, lodPower);
        }

        public void SetRootChild(IChunkGroupOrganizer child)
        {
            this.child = child;
        }

        public bool RemoveChunkAt(Vector3Int pos)
        {
            return child.RemoveChunkAt(pos);
        }

        public bool HasChild => child != null;

        public bool IsEmpty => child == null;
    }
}