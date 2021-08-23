using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupRoot : BaseChunkGroupOrganizer, IChunkGroupRoot
    {

        public ChunkGroupRoot(Vector3Int coord)
        {
            GroupAnchorPosition = coord * MarchingCubeChunkHandler.CHUNK_GROUP_SIZE;
        }

        protected IChunkGroupOrganizer child;

        public override int Size => MarchingCubeChunkHandler.CHUNK_GROUP_SIZE;

        public override Vector3Int GroupRelativeAnchorPosition => GroupAnchorPosition;

        public override IMarchingCubeChunk GetChunkAtLocalPosition(Vector3Int pos)
        {
            return child.GetChunkAtLocalPosition(pos);
        }

        public override bool TryGetChunkAtLocalPosition(Vector3Int pos, out IMarchingCubeChunk chunk) => child.TryGetChunkAtLocalPosition(pos, out chunk);

        public void SetChunkAtPosition(Vector3Int pos, IMarchingCubeChunk chunk)
        {
            SetChunkAtLocalPosition(pos - GroupAnchorPosition, chunk);
        }
            
        public override void SetChunkAtLocalPosition(Vector3Int localPosition, IMarchingCubeChunk chunk)
        {
            if (!HasChild)
            {
                if(chunk.ChunkSize == Size)
                {
                    child = new ChunkGroupTreeLeaf(chunk, GroupAnchorPosition, GroupAnchorPosition);
                }
                else
                {
                    child = new ChunkGroupTreeNode(GroupAnchorPosition, localPosition, Size);
                }
            }
            child.SetChunkAtLocalPosition(localPosition, chunk);
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