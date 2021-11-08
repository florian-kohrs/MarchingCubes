using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    [System.Serializable]
    public abstract class GenericTreeLeaf<T> : BaseChunkGroupOrganizer<T>
    {

        public GenericTreeLeaf()
        {
        }

        public GenericTreeLeaf(T leaf, int index, int[] relativeAnchorPoint, int[] anchorPoint, int sizePower)
        {
            childIndex = index;
            this.sizePower = sizePower;
            this.leaf = leaf;
            GroupAnchorPosition = anchorPoint;
            groupRelativeAnchorPosition = relativeAnchorPoint;
        }

        public override bool IsLeaf => true;


        [Save]
        protected int sizePower;

        [Save]
        protected int childIndex;

        [Save]
        public T leaf;

        [Save]
        public int[] groupRelativeAnchorPosition;

        public override int[] GroupRelativeAnchorPosition => groupRelativeAnchorPosition;

        public bool IsEmpty => leaf != null;

        public override int SizePower => sizePower;


        public override T GetChunkAtLocalPosition(int[] pos)
        {
            return leaf;
        }

        public override void SetChunkAtLocalPosition(int[] pos, T chunk, bool allowOverride)
        {
        }

        public override bool TryGetLeafAtLocalPosition(int[] pos, out T leaf)
        {
            leaf = this.leaf;
            return true;
        }

        public override bool HasChunkAtLocalPosition(int[] pos)
        {
            return true;
        }

    }
}