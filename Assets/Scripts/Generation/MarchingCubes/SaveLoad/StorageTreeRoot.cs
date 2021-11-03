using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    [System.Serializable]
    public class StorageTreeRoot : GenericTreeRoot<StoredChunkEdits, StorageTreeLeaf, IStorageGroupOrganizer<StoredChunkEdits>>
    {

        public StorageTreeRoot() { }

        public StorageTreeRoot(int[] coord) : base(coord, MarchingCubeChunkHandler.STORAGE_GROUP_SIZE)
        {
        }

        public override int Size => MarchingCubeChunkHandler.STORAGE_GROUP_SIZE;

        public override int SizePower => MarchingCubeChunkHandler.STORAGE_GROUP_SIZE_POWER;


        public bool TryGetMipMapOfChunkSizePower(int[] relativePosition, int sizePow, out float[] storedNoise)
        {
            if (child != null)
            {
                return child.TryGetMipMapOfChunkSizePower(relativePosition, sizePow, out storedNoise);
            }
            else
            {
                storedNoise = null;
                return false;
            }
        }

        public override IStorageGroupOrganizer<StoredChunkEdits> GetLeaf(StoredChunkEdits leaf, int index, int[] anchor, int[] relAnchor, int sizePow)
        {
            return new StorageTreeLeaf(leaf, index, anchor, relAnchor, sizePow);
        }

        public override IStorageGroupOrganizer<StoredChunkEdits> GetNode(int[] anchor, int[] relAnchor, int sizePow)
        {
            return new StorageTreeNode(anchor, relAnchor, sizePow);
        }


    }
}