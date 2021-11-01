using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    [System.Serializable]
    public class StorageTreeRoot : GenericTreeRoot<StoredChunkEdits, StorageTreeLeaf, IChunkGroupOrganizer<StoredChunkEdits>>
    {

        public StorageTreeRoot() { }

        public StorageTreeRoot(int[] coord) : base(coord)
        {
        }

        public override IChunkGroupOrganizer<StoredChunkEdits> GetLeaf(StoredChunkEdits leaf, int index, int[] anchor, int[] relAnchor, int sizePow)
        {
            return new StorageTreeLeaf(leaf, index, anchor, relAnchor, sizePow);
        }

        public override IChunkGroupOrganizer<StoredChunkEdits> GetNode(int[] anchor, int[] relAnchor, int sizePow)
        {
            return new StorageTreeNode(anchor, relAnchor, sizePow);
        }


    }
}