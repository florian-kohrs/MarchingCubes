using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{


    [System.Serializable]
    public abstract class GenericTreeNode<T, Node, Leaf, Self> : BaseChunkGroupOrganizer<T>
        where Node : IChunkGroupOrganizer<T>
        where T : ISizeManager
        where Leaf : IHasValue<T>, Node
        where Self : GenericTreeNode<T, Node, Leaf, Self>, Node
    {

        public GenericTreeNode() { }

        public GenericTreeNode(
        Self parent,
        int[] anchorPosition,
        int[] relativeAnchorPosition,
        int index,
        int sizePower)
        {
            this.parent = parent;
            this.index = index;
            this.sizePower = sizePower;
            halfSize = (int)Mathf.Pow(2, sizePower) / 2;
            GroupAnchorPosition = anchorPosition;
            groupRelativeAnchorPosition = relativeAnchorPosition;
        }

        [Save]
        private Self parent;

        public Self Parent => parent;

        public override bool IsLeaf => false;

        [Save]
        public Node[] children = new Node[8];

        [Save]
        public int sizePower;

        protected int ChildrenSizePower => sizePower - 1;


        [Save]
        public int index;

        public int Index => index;

        [Save]
        protected int halfSize;

        [Save]
        protected int[] groupRelativeAnchorPosition;

        public override int[] GroupRelativeAnchorPosition => groupRelativeAnchorPosition;

        public bool IsRoot => parent == null;

        protected const int topLeftBack = 0;
        protected const int topLeftFront = 1;

        protected abstract Self GetSelf { get; }


        public bool HasChildAtIndex(int index) => children[index] != null;

        public bool HasLeafAtIndex(int index) => children[index] is Leaf;

        public abstract Leaf GetLeaf(T leaf, int index, int[] anchor, int[] relAnchor, int sizePow);

        public abstract Self GetNode(int index, int[] anchor, int[] relAnchor, int sizePow);

        public override int SizePower
        {
            get
            {
                return sizePower;
            }
        }

        protected bool HasDirectionAvailable(Direction d, int[] childLocalPosition)
        {
            switch (d)
            {
                case Direction.Right:
                    return childLocalPosition[0] == 0;
                case Direction.Left:
                    return childLocalPosition[0] > 0;
                case Direction.Up:
                    return childLocalPosition[1] == 0;
                case Direction.Down:
                    return childLocalPosition[1] > 0;
                case Direction.Front:
                    return childLocalPosition[2] == 0;
                case Direction.Back:
                    return childLocalPosition[2] > 0;
                default:
                    throw new Exception("Unhandled enum case!");
            } 
        }

        protected static readonly int[] DIRECTION_TO_NEW_CHILD_INDEX_LOOKUP = new int[] { 2, -2, 4, -4, 1, -1 };

        protected int DirectionToNewChildIndex(Direction d, int oldChildIndex, int sign)
        {
            return oldChildIndex + DIRECTION_TO_NEW_CHILD_INDEX_LOOKUP[(int)d] * sign;
        }

        protected void DirectionToNewLocalAndGlobalPosition(Direction d, int oldChildIndex, out int[] localPos, out int[] globalPos)
        {
            localPos = children[oldChildIndex].GroupRelativeAnchorPosition;
            globalPos = children[oldChildIndex].GroupAnchorPositionCopy;
            DirectionHelper.OffsetIntArray(d, localPos, halfSize);
            DirectionHelper.OffsetIntArray(d, globalPos, halfSize);  
        }

        protected int[] AnchorPositonShift(int[] shift)
        {
            return new int[] { GroupAnchorPosition[0] + shift[0], GroupAnchorPosition[1] + shift[1], GroupAnchorPosition[2] + shift[2] };
        }

      
        protected int GetIndexForLocalPosition(int[] position)
        {
            int result = 0;
            if (position[2] >= halfSize) result |= 1;
            if (position[0] >= halfSize) result |= 2;
            if (position[1] >= halfSize) result |= 4;
            return result;
        }

        protected int[] GetLocalPositionFromIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return new int[] {0,0,0};
                case 1:
                    return new int[] { 0, 0, halfSize };
                case 2:
                    return new int[] { halfSize, 0, 0 };
                case 3:
                    return new int[] { halfSize, 0, halfSize };
                case 4:
                    return new int[] { 0, halfSize, 0 };
                case 5:
                    return new int[] { 0, halfSize, halfSize };
                case 6:
                    return new int[] { halfSize, halfSize, 0 };
                case 7:
                    return new int[] { halfSize, halfSize, halfSize };
                default:
                    throw new ArgumentException("Bad index value:" + index);
            }
        }

       

        protected Vector3 GetDirectionFromIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return Vector3.zero;
                case 1:
                    return Vector3.forward;
                case 2:
                    return Vector3.right;
                case 3:
                    return new Vector3(1, 0, 1);
                case 4:
                    return Vector3.up;
                case 5:
                    return new Vector3(0, 1, 1);
                case 6:
                    return new Vector3(1, 1, 0);
                case 7:
                    return new Vector3(1, 1, 1);
                default:
                    throw new ArgumentException("Bad index value:" + index);
            }
        }

        public int ChildCount { get; protected set; }

        public bool HasChildren => ChildCount > 0;

        public void RemoveChildAtIndex(int index)
        {
            children[index] = default;
            ChildCount--;
        }

        protected Vector3 GetChildLocalPositionForIndex(int index)
        {
            return GetOffsetedAnchorPosition(index, 0);
        }

        protected Vector3 GetChildCenterPositionForIndex(int index)
        {
            return GetOffsetedAnchorPosition(index, halfSize / 2);
        }

        private Vector3 GetOffsetedAnchorPosition(int index, int offset)
        {
            Vector3 result = GroupAnchorPositionVector + new Vector3(offset,offset,offset);
            if (index >= 4)
            {
                result.y += halfSize;
                index -= 4;
            }
            if (index >= 2)
            {
                result.x += halfSize;
                index -= 2;
            }
            if (index >= 1)
            {
                result.z += halfSize;
            }
            return result;
        }

        protected void GetAnchorPositionsForChildAtIndex(int childIndex, out int[] anchorPos, out int[] relAchorPos)
        {
            relAchorPos = GetLocalPositionFromIndex(childIndex);
            anchorPos = new int[] {
                relAchorPos[0] + GroupAnchorPosition[0],
                relAchorPos[1] + GroupAnchorPosition[1],
                relAchorPos[2] + GroupAnchorPosition[2]
            };
        }

        protected void GetAnchorPositionForChunkAt(int[] position, out int[] anchorPos, out int[] relAchorPos)
        {
            relAchorPos = new int[3];
            if (position[2] >= halfSize) relAchorPos[2] += halfSize;
            if (position[0] >= halfSize) relAchorPos[0] += halfSize;
            if (position[1] >= halfSize) relAchorPos[1] += halfSize;
            anchorPos = new int[] {
                relAchorPos[0] + GroupAnchorPosition [0],
                relAchorPos[1] + GroupAnchorPosition[1],
                relAchorPos[2] + GroupAnchorPosition[2]
            };
        }

        public override void SetLeafAtLocalPosition(int[] relativePosition, T chunk, bool allowOverride)
        {
            relativePosition[0] -= groupRelativeAnchorPosition[0];
            relativePosition[1] -= groupRelativeAnchorPosition[1];
            relativePosition[2] -= groupRelativeAnchorPosition[2];
            int childIndex = GetIndexForLocalPosition(relativePosition);

            if (chunk.ChunkSizePower >= sizePower - 1 && (children[childIndex] == null || allowOverride))
            {
                int[] childAnchorPosition;
                int[] childRelativeAnchorPosition;
                GetAnchorPositionForChunkAt(relativePosition, out childAnchorPosition, out childRelativeAnchorPosition);
                children[childIndex] = GetLeaf(chunk, childIndex, childAnchorPosition, childRelativeAnchorPosition, sizePower - 1);
            }
            else
            {
                Node child = GetOrCreateChildAt(childIndex, relativePosition, allowOverride);
                child.SetLeafAtLocalPosition(relativePosition, chunk, allowOverride);
            }
        }

        public void OverrideChildAtLocalIndex(int index, T chunk)
        {
            children[index] = GetLeaf(chunk, index, children[index].GroupAnchorPosition, children[index].GroupRelativeAnchorPosition, sizePower - 1);
        }

        public void SetLeafAtLocalIndex(int index, T chunk)
        {
            GetAnchorPositionsForChildAtIndex(index, out int[] anchorPos, out int[] relativePosition);
            SetNewChildAt(GetLeaf(chunk, index, anchorPos, relativePosition, sizePower - 1), index);
        }

        protected void SetNewChildAt(Node child, int index)
        {
            children[index] = child;
            ChildCount++;
        }

        protected Node GetOrCreateChildAt(int index, int[] relativePosition, bool allowOverride)
        {
            if (children[index] == null || (allowOverride && children[index].IsLeaf))
            {
                int[] childAnchorPosition;
                int[] childRelativeAnchorPosition;
                GetAnchorPositionForChunkAt(relativePosition, out childAnchorPosition, out childRelativeAnchorPosition);
                SetNewChildAt(GetNode(index, childAnchorPosition, childRelativeAnchorPosition, sizePower - 1), index);
            }
            return children[index];
        }

        public override bool TryGetLeafAtLocalPosition(int[] localPosition, out T chunk)
        {
            localPosition[0] -= groupRelativeAnchorPosition[0];
            localPosition[1] -= groupRelativeAnchorPosition[1];
            localPosition[2] -= groupRelativeAnchorPosition[2];
            Node child = children[GetIndexForLocalPosition(localPosition)];
            if (child == null)
            {
                chunk = default;
                return false;
            }
            else
            {
                return child.TryGetLeafAtLocalPosition(localPosition, out chunk);
            }
        }

        public override bool RemoveLeafAtLocalPosition(int[] relativePosition)
        {
            relativePosition[0] -= groupRelativeAnchorPosition[0];
            relativePosition[1] -= groupRelativeAnchorPosition[1];
            relativePosition[2] -= groupRelativeAnchorPosition[2];
            Node child = children[GetIndexForLocalPosition(relativePosition)];
            if (child is ChunkGroupTreeLeaf)
            {
                children[GetIndexForLocalPosition(relativePosition)] = default;
                return true;
            }
            else
            {
                return child != null && child.RemoveLeafAtLocalPosition(relativePosition);
            }
        }

        public override bool HasChunkAtLocalPosition(int[] relativePosition)
        {
            relativePosition[0] -= groupRelativeAnchorPosition[0];
            relativePosition[1] -= groupRelativeAnchorPosition[1];
            relativePosition[2] -= groupRelativeAnchorPosition[2];
            Node child = children[GetIndexForLocalPosition(relativePosition)];

            return (child == null && (child is Leaf || child.HasChunkAtLocalPosition(relativePosition)));
        }

    }
}