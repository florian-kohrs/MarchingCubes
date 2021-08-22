using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupRoot : BaseChunkGroupOrganizer, IChunkGroupRoot
    {


        protected IChunkGroupOrganizer child;

        public override int Size => MarchingCubeChunkHandler.CHUNK_GROUP_SIZE;

        public override Vector3Int GroupRelativeAnchorPosition => GroupAnchorPosition;

        public override IMarchingCubeChunk GetChunkAtLocalPosition(Vector3Int pos)
        {
            return child.GetChunkAtLocalPosition(pos);
        }

        public override bool TryGetChunkAtLocalPosition(Vector3Int pos, out IMarchingCubeChunk chunk) => child.TryGetChunkAtLocalPosition(pos, out chunk);

        public void SetChunkAtGlobalPosition(Vector3Int globalPosition, int size, int lodPower, IMarchingCubeChunk chunk)
        {
            SetChunkAtLocalPosition(globalPosition - GroupAnchorPosition, size, lodPower, chunk);
        }
            
        public override void SetChunkAtLocalPosition(Vector3Int localPosition, int size, int lodPower, IMarchingCubeChunk chunk)
        {
            if (!HasChild)
            {
                if(size == Size)
                {
                    child = new ChunkGroupTreeLeaf(ChunkBuilder, GroupAnchorPosition, GroupAnchorPosition, size, lodPower);
                }
                else
                {
                    child = new ChunkGroupTreeNode(ChunkBuilder, GroupAnchorPosition, localPosition, size);
                }
            }
            child.SetChunkAtLocalPosition(localPosition, size, lodPower, chunk);
        }

        public bool RemoveChunkAtGlobalPosition(Vector3Int pos)
        {
            return child.RemoveChunkAtLocalPosition(pos - GroupAnchorPosition);
        }


        public override bool RemoveChunkAtLocalPosition(Vector3Int pos)
        {
            return child.RemoveChunkAtLocalPosition(pos);
        }

        public override bool HasChunkAtLocalPosition(Vector3Int pos)
        {
            return child != null && child.HasChunkAtLocalPosition(pos);
        }

        public bool HasChunkAtGlobalPosition(Vector3Int globalPosition)
        {
            return HasChunkAtLocalPosition(globalPosition - GroupAnchorPosition);
        }

        public bool HasChild => child != null;

        public bool IsEmpty => child == null;
    }
}