using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    [System.Serializable]
    public class StorageTreeRoot : GenericTreeRoot<StoredChunkEdits, StorageTreeLeaf, IStorageGroupOrganizer<StoredChunkEdits>>
    {

        public StorageTreeRoot() { }

        public StorageTreeRoot(int[] coord, int size) : base(coord, size)
        {
        }

        public override int Size => MarchingCubeChunkHandler.STORAGE_GROUP_SIZE;

        public override int SizePower => MarchingCubeChunkHandler.STORAGE_GROUP_SIZE_POWER;


        public bool TryGetMipMapOfChunkSizePower(int[] relativePosition, int sizePow, out float[] storedNoise, out bool isMipMapComplete)
        {
            IStorageGroupOrganizer<StoredChunkEdits> node;
            if (TryGetNodeWithSizePower(relativePosition, sizePow, out node))
            {
                storedNoise = node.NoiseMap;
                isMipMapComplete = node.IsMipMapComplete;
            }
            else
            {
                storedNoise = null;
                isMipMapComplete = false;
            }

            return storedNoise != null;
        }


        public bool TryGetNodeWithSizePower(int[] relativePosition, int sizePow, out IStorageGroupOrganizer<StoredChunkEdits> child)
        {
            if (this.child == null)
            {
                child = default;
                return false;
            }
            else
            {
                return this.child.TryGetNodeWithSizePower(relativePosition, sizePow, out child);
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
