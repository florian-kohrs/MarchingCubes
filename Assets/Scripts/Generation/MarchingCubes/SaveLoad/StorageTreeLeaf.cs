using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    [System.Serializable]
    public class StorageTreeLeaf : GenericTreeLeaf<StoredChunkEdits>
    {
        public StorageTreeLeaf() { }

        public StorageTreeLeaf(StoredChunkEdits leaf, int index, int[] relativeAnchorPoint, int[] anchorPoint, int sizePower) : base(leaf, index, relativeAnchorPoint, anchorPoint, sizePower)
        {
        }

        public override bool RemoveLeafAtLocalPosition(int[] pos)
        {
            return false;
        }

    }
}