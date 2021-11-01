using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MarchingCubes
{

    public interface IChunkGroupParent
    {

        void SplitChild(ChunkGroupTreeLeaf leaf, int index, IMarchingCubeChunk chunk, IMarchingCubeChunkHandler chunkHandler);

        ChunkGroupTreeLeaf[] GetLeafs();

        bool AreAllChildrenLeafs(int targetLodPower);

        int[] GroupRelativeAnchorPosition { get; }

        int[] GroupAnchorPosition { get; }
        Vector3Int GroupAnchorPositionVector { get; }

    }

}
