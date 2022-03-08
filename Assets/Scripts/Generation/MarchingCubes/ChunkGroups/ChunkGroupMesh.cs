using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupMesh : GroupMesh<ChunkGroupRoot, CompressedMarchingCubeChunk, ChunkGroupTreeLeaf, IChunkGroupOrganizer<CompressedMarchingCubeChunk>>
    {

        public ChunkGroupMesh(int groupSize) : base(groupSize) { }


        protected override ChunkGroupRoot CreateKey(Vector3Int coord)
        {
            return new ChunkGroupRoot(new int[] { coord.x, coord.y, coord.z }, GROUP_SIZE);
        }


        public bool TryGetReadyChunkAt(int[] p, out CompressedMarchingCubeChunk chunk)
        {
            return TryGetGroupItemAt(p, out chunk) && chunk.IsReady;
        }

        public bool HasChunkStartedAt(int[] p)
        {
            return TryGetGroupItemAt(p, out CompressedMarchingCubeChunk chunk) && chunk.HasStarted;
        }


    }
}