using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupMesh : GroupMesh<ChunkGroupRoot, ICompressedMarchingCubeChunk, ChunkGroupTreeLeaf, IChunkGroupOrganizer<ICompressedMarchingCubeChunk>>
    {

        public ChunkGroupMesh(int groupSize) : base(groupSize) { }


        protected override ChunkGroupRoot CreateKey(Vector3Int coord)
        {
            return new ChunkGroupRoot(new int[] { coord.x, coord.y, coord.z }, GROUP_SIZE);
        }


        public bool TryGetReadyChunkAt(Vector3Int p, out ICompressedMarchingCubeChunk chunk)
        {
            return TryGetGroupItemAt(p, out chunk) && chunk.IsReady;
        }

        public bool HasChunkStartedAt(Vector3Int p)
        {
            return TryGetGroupItemAt(p, out ICompressedMarchingCubeChunk chunk) && chunk.HasStarted;
        }


    }
}