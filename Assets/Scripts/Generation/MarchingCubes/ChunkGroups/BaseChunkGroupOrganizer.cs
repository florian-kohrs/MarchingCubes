using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public abstract class BaseChunkGroupOrganizer : IChunkGroupOrganizer
    {

        public Vector3Int GroupAnchorPosition { get; set; }

        public IChunkBuilder ChunkBuilder { protected get; set; }

        public abstract Vector3Int GroupRelativeAnchorPosition { get; }

        public abstract int Size { get; }

        public abstract IMarchingCubeChunk GetChunkAtLocalPosition(Vector3Int pos);
        public abstract void SetChunkAtLocalPosition(Vector3Int pos, IMarchingCubeChunk chunk);
        public abstract bool TryGetChunkAtLocalPosition(Vector3Int pos, out IMarchingCubeChunk chunk);
        public abstract bool HasChunkAtLocalPosition(Vector3Int pos);
        public abstract bool RemoveChunkAtLocalPosition(Vector3Int pos);
    }
}