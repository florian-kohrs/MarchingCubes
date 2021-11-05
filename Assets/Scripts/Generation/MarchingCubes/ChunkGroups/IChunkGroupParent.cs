using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MarchingCubes
{

    public interface IChunkGroupParent<T> : ITreeNodeParent<T>
    {

        void SplitLeaf(int index);

        int[][] GetAllChildGlobalAnchorPosition();

    }

}
