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

        public int ChildrenWithMipMapReady => throw new System.NotImplementedException();

        public int DirectNonNullChildren => throw new System.NotImplementedException();

        public bool HasNoiseMapReady => true;

        public override bool RemoveLeafAtLocalPosition(int[] pos)
        {
            return false;
        }


        public bool TryGetNodeWithSizePower(int[] relativePosition, int sizePow, out IStorageGroupOrganizer<StoredChunkEdits> child)
        {
            if(sizePow == sizePower)
                child = this;
            else
                child = null;

            return child != null;
        }


        public bool TryGetMipMapOfChunkSizePower(int[] relativePosition, int sizePow, out float[] storedNoise, out bool isMipMapComplete)

        {
            storedNoise = NoiseMap;
            isMipMapComplete = true;
            return true;
        }

    }
}
