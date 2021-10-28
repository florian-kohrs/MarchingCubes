using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public abstract class BaseChunkGroupOrganizer : IChunkGroupOrganizer
    {

        public int[] GroupAnchorPosition { get; set; }

        public IChunkBuilder ChunkBuilder { protected get; set; }

        public abstract int[] GroupRelativeAnchorPosition { get; }

        public abstract int Size { get; }

        public abstract IMarchingCubeChunk GetChunkAtLocalPosition(int[] pos);
        public abstract void SetChunkAtLocalPosition(int[] pos, IMarchingCubeChunk chunk, bool allowOverride);
        public abstract bool TryGetChunkAtLocalPosition(int[] pos, out IMarchingCubeChunk chunk);
        public abstract bool HasChunkAtLocalPosition(int[] pos);
        public abstract bool RemoveChunkAtLocalPosition(int[] pos);
    }
}