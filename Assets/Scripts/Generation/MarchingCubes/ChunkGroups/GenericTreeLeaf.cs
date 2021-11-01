using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public abstract class GenericTreeLeaf<T,P> : BaseChunkGroupOrganizer<T>
    {

        public GenericTreeLeaf(P parent, T leaf, int index, int[] relativeAnchorPoint, int[] anchorPoint, int sizePower)
        {
            childIndex = index;
            this.parent = parent;
            this.sizePower = sizePower;
            this.leaf = leaf;
            groupRelativeAnchorPosition = relativeAnchorPoint;
        }

        protected int sizePower;

        protected int childIndex;

        public P parent;

        public T leaf;

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