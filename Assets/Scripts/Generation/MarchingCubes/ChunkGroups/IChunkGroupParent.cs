using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MarchingCubes
{

    public interface IChunkGroupParent<T> : ITreeNodeParent<T>
    {

        void RemoveChildAtIndex(int index, IMarchingCubeChunk chunk);

        bool EntireHirachyHasAtLeastTargetLod(int targetLodPower);

        void PrepareBranchDestruction(List<IMarchingCubeChunk> oldChunks);

    }

}
