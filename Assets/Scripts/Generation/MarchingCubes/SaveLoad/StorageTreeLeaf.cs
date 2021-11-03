using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    [System.Serializable]
    public class StorageTreeLeaf : GenericTreeLeaf<StoredChunkEdits>, IStorageGroupOrganizer<StoredChunkEdits>
    {

        public StorageTreeLeaf() { }

        public StorageTreeLeaf(StoredChunkEdits leaf, int index, int[] relativeAnchorPoint, int[] anchorPoint, int sizePower) : base(leaf, index, relativeAnchorPoint, anchorPoint, sizePower)
        {
        }

        public float[] NoiseMap => leaf.vals;

        public override bool RemoveLeafAtLocalPosition(int[] pos)
        {
            return false;
        }

        public bool TryGetMipMapOfChunkSizePower(int[] relativePosition, int sizePow, out float[] storedNoise)
        {
            throw new System.Exception("Cant get mitmap of leaf. Dont request mipmap for sizepower <= 5!");
        }
    }
}