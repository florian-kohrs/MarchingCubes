using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MarchingCubes
{

    [System.Serializable]
    public class StorageTreeNode : GenericTreeNode<StoredChunkEdits, IChunkGroupOrganizer<StoredChunkEdits>, IChunkGroupOrganizer<StoredChunkEdits>>
    {

        public StorageTreeNode() { }

        public StorageTreeNode(
           int[] anchorPosition,
           int[] relativeAnchorPosition,
           int sizePower) : base(anchorPosition, relativeAnchorPosition, sizePower)
        {
        }

        public override bool AreAllChildrenLeafs(int targetLodPower)
        {
            return sizePower == MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE_POWER + 1;
        }

        public override IChunkGroupOrganizer<StoredChunkEdits> GetLeaf(StoredChunkEdits leaf, int index, int[] anchor, int[] relAnchor, int sizePow)
        {
            return new StorageTreeLeaf(leaf, index, anchor, relAnchor,sizePow);
        }

        public override IChunkGroupOrganizer<StoredChunkEdits>[] GetLeafs()
        {
            return children;
        }

        public override IChunkGroupOrganizer<StoredChunkEdits> GetNode(int[] anchor, int[] relAnchor, int sizePow)
        {
            return new StorageTreeNode(anchor, relAnchor, sizePow);
        }

    }

}