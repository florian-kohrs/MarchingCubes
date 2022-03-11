using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    [System.Serializable]
    public class StorageTreeLeaf : GenericTreeLeaf<StoredChunkEdits>, IStorageGroupOrganizer<StoredChunkEdits>
    {

        public StorageTreeLeaf() { }

        public StorageTreeLeaf(IStorageGroupOrganizer<StoredChunkEdits> parent, StoredChunkEdits leaf, int index, int[] relativeAnchorPoint, int[] anchorPoint, int sizePower) : base(leaf, index, relativeAnchorPoint, anchorPoint, sizePower)
        {
            leaf.leaf = this;
            this.parent = parent;
        }

        //TODO: Maybe store for each chunk num of tris -> dont have to read from gpu

        public float[] NoiseMap => leaf.noise;

        public int ChildrenWithMipMapReady => throw new System.NotImplementedException();

        public int DirectNonNullChildren => throw new System.NotImplementedException();

        public bool HasNoiseMapReady => true;

        public bool IsMipMapComplete => true;

        public IStorageGroupOrganizer<StoredChunkEdits> parent;

        public override void SetLeafAtLocalPosition(int[] pos, StoredChunkEdits chunk, bool allowOverride)
        {
            chunk.leaf = this;
        }

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

        public void RemoveMipMapInHirachy()
        {
            if(parent != null)
                parent.RemoveMipMapInHirachy();
        }

    }
}
