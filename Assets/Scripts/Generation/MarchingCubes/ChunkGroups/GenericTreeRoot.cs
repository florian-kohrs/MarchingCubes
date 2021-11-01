using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    [System.Serializable]
    public abstract class GenericTreeRoot<T,Leaf, Child> : IChunkGroupRoot<T>, IChunkGroupParent<Leaf> where T : ISizeManager where Child : IChunkGroupOrganizer<T> 
    {

        public GenericTreeRoot() { }

        public GenericTreeRoot(int[] coord)

        {
            GroupAnchorPosition = new int[]{
            coord[0] * MarchingCubeChunkHandler.CHUNK_GROUP_SIZE,
            coord[1] * MarchingCubeChunkHandler.CHUNK_GROUP_SIZE,
            coord[2] * MarchingCubeChunkHandler.CHUNK_GROUP_SIZE
            };
        }

        [Save]
        protected Child child;

        public int Size => MarchingCubeChunkHandler.CHUNK_GROUP_SIZE;

        public int SizePower => MarchingCubeChunkHandler.CHUNK_GROUP_SIZE_POWER;

        [Save]
        public int[] groupAnchorPosition;

        public int[] GroupAnchorPosition { get { return groupAnchorPosition; } set { groupAnchorPosition = value; } }

        public bool HasChild => child != null;

        public int[] GroupRelativeAnchorPosition => GroupAnchorPosition;

        public Vector3Int GroupAnchorPositionVector { get => new Vector3Int(GroupAnchorPosition[0], GroupAnchorPosition[1], GroupAnchorPosition[2]); }

        public bool HasChunkAtGlobalPosition(int[] pos)
        {
            return child != null && child.HasChunkAtLocalPosition(pos);
        }

        public bool RemoveChunkAtGlobalPosition(int[] pos)
        {
            return child.RemoveLeafAtLocalPosition(pos);
        }

        public bool RemoveChunkAtGlobalPosition(Vector3Int pos)
        {
            return RemoveChunkAtGlobalPosition(new int[] { pos.x, pos.y, pos.z });
        }

        public abstract Child GetLeaf(T leaf, int index, int[] anchor, int[] relAnchor, int sizePow);

        public abstract Child GetNode(int[] anchor, int[] relAnchor, int sizePow);

        public void SetLeafAtPosition(int[] pos, T leaf, bool allowOverride)
        {
            if (!HasChild || allowOverride)
            {
                if (leaf.ChunkSizePower == SizePower)
                {
                    child = GetLeaf(leaf, 0, GroupAnchorPosition, GroupAnchorPosition, SizePower);
                }
                else
                {
                    child = GetNode(GroupAnchorPosition, GroupAnchorPosition, SizePower);
                }
            }
            child.SetChunkAtLocalPosition(pos, leaf, allowOverride);
        }

        public bool TryGetLeafAtGlobalPosition(Vector3Int pos, out T chunk)
        {
            return child.TryGetLeafAtLocalPosition(new int[] { pos.x, pos.y, pos.z }, out chunk);
        }

        public Leaf[] GetLeafs()
        {
            return null;
        }

        public bool AreAllChildrenLeafs(int targetLodPower)
        {
            return false;
        }
    }
}
