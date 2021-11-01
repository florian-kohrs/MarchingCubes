using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupRoot : IChunkGroupRoot, IChunkGroupParent
    {

        public ChunkGroupRoot(int[] coord)
        {
            GroupAnchorPosition = new int[]{
            coord[0] * MarchingCubeChunkHandler.CHUNK_GROUP_SIZE,
            coord[1] * MarchingCubeChunkHandler.CHUNK_GROUP_SIZE,
            coord[2] * MarchingCubeChunkHandler.CHUNK_GROUP_SIZE
            };
        }
        public int[] GroupAnchorPosition { get; set; }

        public IChunkBuilder ChunkBuilder { protected get; set; }

        protected IChunkGroupOrganizer child;

        public int Size => MarchingCubeChunkHandler.CHUNK_GROUP_SIZE;

        public int SizePower => MarchingCubeChunkHandler.CHUNK_GROUP_SIZE_POWER;

        public int[] GroupRelativeAnchorPosition => GroupAnchorPosition;

        public Vector3Int GroupAnchorPositionVector { get => new Vector3Int(GroupAnchorPosition[0], GroupAnchorPosition[1], GroupAnchorPosition[2]); }

        public IMarchingCubeChunk GetChunkAtLocalPosition(int[] pos)
        {
            return child.GetChunkAtLocalPosition(pos);
        }

        public bool TryGetChunkAtLocalPosition(int[] pos, out IMarchingCubeChunk chunk) => child.TryGetChunkAtLocalPosition(pos, out chunk);

        public void SetChunkAtPosition(int[] pos, IMarchingCubeChunk chunk, bool allowOverride)
        {
            if (!HasChild || allowOverride)
            {
                if (chunk.ChunkSizePower == SizePower)
                {
                    child = new ChunkGroupTreeLeaf(this, chunk, 0, GroupAnchorPosition, GroupAnchorPosition, SizePower);
                }
                else
                {
                    child = new ChunkGroupTreeNode(GroupAnchorPosition, GroupAnchorPosition, SizePower);
                }
            }
            child.SetChunkAtLocalPosition(pos, chunk, allowOverride);
        }
            

        public bool RemoveChunkAtGlobalPosition(int[] pos)
        {
            return child.RemoveChunkAtLocalPosition(pos);
        }


        public bool HasChunkAtGlobalPosition(int[] globalPosition)
        {
            return child != null && child.HasChunkAtLocalPosition(globalPosition);
        }

        public bool TryGetChunkAtGlobalPosition(int[] pos, out IMarchingCubeChunk chunk)
        {
            return child.TryGetChunkAtLocalPosition(pos, out chunk);
        }

        public bool TryGetChunkAtGlobalPosition(Vector3Int pos, out IMarchingCubeChunk chunk)
        {
            return TryGetChunkAtGlobalPosition(new int[]{ pos.x,pos.y,pos.z}, out chunk);
        }

        public bool RemoveChunkAtGlobalPosition(Vector3Int pos)
        {
            return RemoveChunkAtGlobalPosition(new int[] { pos.x, pos.y, pos.z });
        }

        public void SplitChild(ChunkGroupTreeLeaf leaf, int index, IMarchingCubeChunk chunk, IMarchingCubeChunkHandler chunkHandler)
        {
            if(chunk.ChunkSizePower == SizePower)
            {
                child = new ChunkGroupTreeNode(GroupAnchorPosition, GroupAnchorPosition, SizePower);
            }
            ((IChunkGroupParent)child).SplitChild(leaf, index, chunk, chunkHandler);
            chunk.ResetChunk();
        }

        public ChunkGroupTreeLeaf[] GetLeafs()
        {
            return null;
        }

        public bool AreAllChildrenLeafs(int _)
        {
            return false;
        }

        public bool HasChild => child != null;

        public bool IsEmpty => child == null;
    }
}