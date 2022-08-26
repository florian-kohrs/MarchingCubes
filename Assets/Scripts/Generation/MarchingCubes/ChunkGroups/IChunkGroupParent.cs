using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MarchingCubes
{

    public interface IChunkGroupParent<T> : ITreeNodeParent<T>
    {

        void RemoveChildAtIndex(int index, CompressedMarchingCubeChunk chunk);

        bool EntireHirachyHasAtLeastTargetLod(int targetLodPower);

        int FindTargetLodThatWorksForHirachyOfAtLeast(int targetLodPower);

        void PrepareBranchDestruction(List<CompressedMarchingCubeChunk> oldChunks);

        IChunkGroupParent<ChunkGroupTreeLeaf> AscendParentHirachy(int steps);

    }

}
