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

        protected abstract Leaf GetLeaf(T leafValue, int index, int[] anchor, int[] relAnchor, int sizePow);

        public Leaf SetLeafAtIndex(T leafValue, int index)
        {
            Leaf result;
            if(children[index] != null)
            {
                result = OverrideWithLeafAtLocalIndex(index, leafValue);
            }
            else 
            {
                ChildCount++;
                GetAnchorPositionsForChildAtIndex(index, out int[] anchor, out int[] relAnchor);
                result = GetLeaf(leafValue, index, anchor, relAnchor, ChildrenSizePower);
                children[index] = result;
            }
            return result;
        }

        protected abstract Self GetNode(int index, int[] anchor, int[] relAnchor, int sizePow);

        public Self SetNodeAt(int index)
        {
            Self result;
            if (children[index] != null)
            {
                result = OverrideWithNodeAtLocalIndex(index);
            }
            else
            {
                ChildCount++;
                GetAnchorPositionsForChildAtIndex(index, out int[] anchor, out int[] relAnchor);
                result = GetNode(index, anchor, relAnchor, ChildrenSizePower);
                children[index] = result;
            }
            return result;
        }


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

       

        protected Vector3 GetDirectionFromIndex(int index, int directionLength)
        {
            switch (index)
            {
                case 0:
                    return new Vector3(0, 0, 0);
                case 1:
                    return new Vector3(0, 0, directionLength);
                case 2:
                    return new Vector3(directionLength, 0, 0);
                case 3:
                    return new Vector3(directionLength, 0, directionLength);
                case 4:
                    return new Vector3(0, directionLength, 0);
                case 5:
                    return new Vector3(0, directionLength, directionLength);
                case 6:
                    return new Vector3(directionLength, directionLength, 0);
                case 7:
                    return new Vector3(directionLength, directionLength, directionLength);
                default:
                    throw new ArgumentException("Bad index value:" + index);
            }
        }

        public int ChildCount { get; protected set; }

        public bool HasChildren => ChildCount > 0;


        protected void GetAnchorPositionsForChildAtIndex(int childIndex, out int[] anchorPos, out int[] relAchorPos)
        {
            relAchorPos = GetLocalPositionFromIndex(childIndex);
            anchorPos = new int[] {
                relAchorPos[0] + GroupAnchorPosition[0],
                relAchorPos[1] + GroupAnchorPosition[1],
                relAchorPos[2] + GroupAnchorPosition[2]
            };
        }

        public bool SetLeafAtPath(Stack<int> path, T value, bool allowOverrideValue)
        {
            int currentChildIndex = path.Pop();
            var child = children[currentChildIndex];
            if(path.Count == 0)
            {
                if(child == null)
                {
                    SetLeafAtIndex(value, currentChildIndex);
                    return true;
                }
                else if(allowOverrideValue && child is Leaf l)
                {
                    l.Value = value;
                    return true;
                }
                return false;
            }
            else if(child == null)
            {
                Self node = SetNodeAt(currentChildIndex);
                return node.SetLeafAtPath(path, value, allowOverrideValue);
            }
            else if(child is Leaf)
            {
                return false;
            }
            else if(child is Self self)
            {
                return self.SetLeafAtPath(path, value, allowOverrideValue);
            }
            return false;
        }

        public override void SetLeafAtLocalPosition(int[] relativePosition, T node, bool allowOverride)
        {
            relativePosition[0] -= groupRelativeAnchorPosition[0];
            relativePosition[1] -= groupRelativeAnchorPosition[1];
            relativePosition[2] -= groupRelativeAnchorPosition[2];
            int childIndex = GetIndexForLocalPosition(relativePosition);

            if (node.NodeSizePower >= sizePower - 1 && (children[childIndex] == null || allowOverride))
            {
                SetLeafAtIndex(node, childIndex);
            }
            else
            {
                Node child = GetOrCreateChildAt(childIndex, allowOverride);
                child.SetLeafAtLocalPosition(relativePosition, node, allowOverride);
            }
        }

        private Leaf OverrideWithLeafAtLocalIndex(int index, T chunk)
        {
            Leaf l = GetLeaf(chunk, index, children[index].GroupAnchorPosition, children[index].GroupRelativeAnchorPosition, ChildrenSizePower);
            children[index] = l;
            return l;
        }

        private Self OverrideWithNodeAtLocalIndex(int index)
        {
            Self l = GetNode(index, children[index].GroupAnchorPosition, children[index].GroupRelativeAnchorPosition, ChildrenSizePower);
            children[index] = l;
            return l;
        }

        public bool TryFollowPathToLeaf(Stack<int> path, out T leafValue)
        {
            int next = path.Pop();
            var child = children[next];
            if(path.Count == 0)
            {
                if (child is Leaf l)
                    leafValue = l.Value;
                else
                    leafValue = default;
            }
            else
            {
                if (child is Self s)
                    return s.TryFollowPathToLeaf(path, out leafValue);
                else
                    leafValue = default;
            }
            return !leafValue.Equals(default);
        }

        protected Node GetOrCreateChildAt(int index, bool allowOverride)
        {
            if (children[index] == null || (allowOverride && children[index].IsLeaf))
            {
                SetNodeAt(index);
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