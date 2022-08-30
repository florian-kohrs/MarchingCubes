using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public enum Direction { Right = 0, Left = 1, Up = 2, Down = 3, Front = 4, Back = 5 }


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

        //public bool TryGetEmptyLeafParentInDirection(Direction d, Stack<int> childIndices, out Self parent)
        //{
        //    return TryGetLeafParentInDirection(d, childIndices, out parent) && !parent.HasChildAtIndex(childIndices.Peek());
        //}

        public bool TryGetEmptyLeafParentInDirection(Direction d, Stack<int> childIndices, out Self parent)
        {
            bool result = false;
            int childIndex = childIndices.Pop();
            int depth = childIndices.Count;
            if (HasDirectionAvailable(d, children[childIndex].GroupRelativeAnchorPosition))
            {
                int newChildIndex = DirectionToNewChildIndex(d, childIndex, 1);
                parent = GetSelf;
                if (depth == 0)
                {
                    childIndices.Push(newChildIndex);
                    result = !HasChildAtIndex(childIndex);
                }
                else
                {
                    Node child = children[newChildIndex];
                    Self node;
                    if (child is Leaf)
                    {
                        node = null;
                        childIndices.Push(childIndex);
                        result = false;
                    }
                    else if (child is Self self)
                    {
                        node = self;
                    }
                    else
                    {
                        ///child is null
                        /////TODO Dont spawn here! Cant be used in async neighbour search!
                        DirectionToNewLocalAndGlobalPosition(d, childIndex, out int[] localPos, out int[] globalPos);
                        node = GetNode(newChildIndex, globalPos, localPos, sizePower - 1);
                        children[childIndex] = node;
                    }

                    if (node != null)
                        return node.TryGetEmptyLeafParentInDirection(d, childIndices, out parent);
                }
                return result;
            }
            else
            {
                int oldSwappedChildIndex = DirectionToNewChildIndex(d, childIndex, -1);
                childIndices.Push(oldSwappedChildIndex);
                childIndices.Push(childIndex);
                return this.parent.TryGetLeafParentInDirection(d, childIndices, out parent);
            }
        }

        public bool HasChildAtIndex(int index) => children[index] != null;

        public bool HasLeafAtIndex(int index) => children[index] is Leaf;

        protected const int topRightBack = 2;
        protected const int topRightFront = 3;
        protected const int bottomLeftBack = 4;
        protected const int bottomLeftFront = 5;
        protected const int bottomRightBack = 6;
        protected const int bottomRightFront = 7;


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

        protected int DirectionToNewChildIndex(Direction d, int oldChildIndex, int sign)
        {
            int newChildIndex = oldChildIndex;
            switch (d)
            {
                case Direction.Right:
                    newChildIndex += 2 * sign;
                    break;
                case Direction.Left:
                    newChildIndex -= 2 * sign;
                    break;
                case Direction.Up:
                    newChildIndex += 4 * sign;
                    break;
                case Direction.Down:
                    newChildIndex -= 4 * sign;
                    break;
                case Direction.Front:
                    newChildIndex += 1 * sign;
                    break;
                case Direction.Back:
                    newChildIndex -= 1 * sign;
                    break;
                default:
                    throw new Exception("Unhandled enum case!");
            }
            return newChildIndex;
        }

        protected void DirectionToNewLocalAndGlobalPosition(Direction d, int oldChildIndex, out int[] localPos, out int[] globalPos)
        {
            localPos = children[oldChildIndex].GroupRelativeAnchorPosition;
            globalPos = children[oldChildIndex].GroupAnchorPositionCopy;
            switch (d)
            {
                case Direction.Right:
                    localPos[0] += halfSize;
                    globalPos[0] += halfSize;
                    break;
                case Direction.Left:
                    localPos[0] -= halfSize;
                    globalPos[0] -= halfSize;
                    break;
                case Direction.Up:
                    localPos[1] += halfSize;
                    globalPos[1] += halfSize;
                    break;
                case Direction.Down:
                    localPos[1] -= halfSize;
                    globalPos[1] -= halfSize;
                    break;
                case Direction.Front:
                    localPos[2] += halfSize;
                    globalPos[2] += halfSize;
                    break;
                case Direction.Back:
                    localPos[2] -= halfSize;
                    globalPos[2] -= halfSize;
                    break;
                default:
                    throw new System.Exception("Unhandled enum case!");
            }
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
            int[] result = new int[3];
            if (index >= 4)
            {
                result[1] += halfSize;
                index -= 4;
            }
            if (index >= 2)
            {
                result[0] += halfSize;
                index -= 2;
            }
            if (index >= 1)
            {
                result[2] += halfSize;
            }
            return result;
        }


        public void RemoveChildAtIndex(int index)
        {
            //TODO:Maybe disallow nodes to be removed here
            children[index] = default;
        }


        protected Vector3 GetChildLocalPositionForIndex(int index)
        {
            return GetChildCenterPositionForIndex(index, 0);
        }

        protected Vector3 GetChildCenterPositionForIndex(int index)
        {
            return GetChildCenterPositionForIndex(index, halfSize / 2);
        }

        private Vector3 GetChildCenterPositionForIndex(int index, int offset)
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

        public override T GetChunkAtLocalPosition(int[] pos)
        {
            IChunkGroupOrganizer<T> child = children[GetIndexForLocalPosition(pos)];
            pos[0] -= groupRelativeAnchorPosition[0];
            pos[1] -= groupRelativeAnchorPosition[1];
            pos[2] -= groupRelativeAnchorPosition[2];
            return child.GetChunkAtLocalPosition(pos);
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
            int[] relativePosition = GetLocalPositionFromIndex(index);
            int[] anchorPos = new int[] {
                relativePosition[0] + GroupAnchorPosition [0],
                relativePosition[1] + GroupAnchorPosition[1],
                relativePosition[2] + GroupAnchorPosition[2]
            };

            children[index] = GetLeaf(chunk, index, anchorPos, relativePosition, sizePower - 1);
        }

        protected Node GetOrCreateChildAt(int index, int[] relativePosition, bool allowOverride)
        {
            if (children[index] == null || (allowOverride && children[index].IsLeaf))
            {
                int[] childAnchorPosition;
                int[] childRelativeAnchorPosition;
                GetAnchorPositionForChunkAt(relativePosition, out childAnchorPosition, out childRelativeAnchorPosition);
                children[index] = GetNode(index, childAnchorPosition, childRelativeAnchorPosition, sizePower - 1);
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