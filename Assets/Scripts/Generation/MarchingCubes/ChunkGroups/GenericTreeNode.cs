using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public enum Direction { Right = 0, Left = 1, Up = 2, Down = 3, Front = 4, Back = 5 }


    [System.Serializable]
    public abstract class GenericTreeNode<T, Node, Leaf, Self> : BaseChunkGroupOrganizer<T>, ITreeNodeParent<Leaf> 
        where Node : IChunkGroupOrganizer<T>
        where T : ISizeManager
        where Self : GenericTreeNode<T, Node, Leaf, Self>
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
        protected const int topRightBack = 2;
        protected const int topRightFront = 3;
        protected const int bottomLeftBack = 4;
        protected const int bottomLeftFront = 5;
        protected const int bottomRightBack = 6;
        protected const int bottomRightFront = 7;


        public abstract Node GetLeaf(T leaf, int index, int[] anchor, int[] relAnchor, int sizePow);

        public abstract Node GetNode(int index, int[] anchor, int[] relAnchor, int sizePow);

        public override int SizePower
        {
            get
            {
                return sizePower;
            }
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
                children[childIndex] = GetLeaf(chunk, childIndex, childRelativeAnchorPosition, childAnchorPosition, sizePower - 1);
            }
            else
            {
                Node child = GetOrCreateChildAt(childIndex, relativePosition, allowOverride);
                child.SetLeafAtLocalPosition(relativePosition, chunk, allowOverride);
            }
        }

        public override void OverrideChildAtLocalIndex(int index, T chunk)
        {
            children[index] = GetLeaf(chunk, index, children[index].GroupRelativeAnchorPosition, children[index].GroupAnchorPosition, sizePower - 1);
        }

        public void SetLeafAtLocalIndex(int index, T chunk)
        {
            int[] relativePosition = GetLocalPositionFromIndex(index);
            int[] anchorPos = new int[] {
                relativePosition[0] + GroupAnchorPosition [0],
                relativePosition[1] + GroupAnchorPosition[1],
                relativePosition[2] + GroupAnchorPosition[2]
            };

            children[index] = GetLeaf(chunk, index, relativePosition, anchorPos, sizePower - 1);
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