using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    [System.Serializable]
    public abstract class GenericTreeLeaf<T,Node, Self, ParentType> : BaseChunkGroupOrganizer<T>, IHasValue<T>
        where ParentType : GenericTreeNode<T,Node, Self, ParentType>, Node
        where T : ISizeManager
        where Node : IChunkGroupOrganizer<T>
        where Self : IHasValue<T>, Node
    {

        public GenericTreeLeaf()
        {
        }

        public GenericTreeLeaf(ParentType parent, T leaf, int index, int[] anchorPoint, int[] relativeAnchorPoint, int sizePower)
        {
            this.leaf = leaf;
            childIndex = index;
            this.parent = parent;
            this.sizePower = sizePower;
            GroupAnchorPosition = anchorPoint;
            groupRelativeAnchorPosition = relativeAnchorPoint;
        }

        public override bool IsLeaf => true;

        [Save]
        protected ParentType parent;

        public T Value { get => leaf; set => SetValue(value); }

        protected abstract void SetValue(T value);

        [Save]
        protected int sizePower;

        [Save]
        protected int childIndex;

        public int ChildIndex => childIndex;

        [Save]
        public T leaf;

        [Save]
        public int[] groupRelativeAnchorPosition;

        public override int[] GroupRelativeAnchorPosition => groupRelativeAnchorPosition;

        public bool IsEmpty => leaf != null;

        public override int SizePower => sizePower;

        public override void SetLeafAtLocalPosition(int[] pos, T chunk, bool allowOverride)
        {
            throw new System.Exception();
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