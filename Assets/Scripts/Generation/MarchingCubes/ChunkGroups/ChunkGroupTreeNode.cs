using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupTreeNode : BaseChunkGroupOrganizer
    {

        public ChunkGroupTreeNode(
            Vector3Int anchorPosition, 
            Vector3Int relativeAnchorPosition, 
            int size)
        {
            this.size = size;
            halfSize = size / 2;
            GroupAnchorPosition = anchorPosition;
            groupRelativeAnchorPosition = relativeAnchorPosition; 
        }

        protected const int topLeftBack = 0;
        protected const int topLeftFront = 1;
        protected const int topRightBack = 2;
        protected const int topRightFront = 3;
        protected const int bottomLeftBack = 4;
        protected const int bottomLeftFront = 5;
        protected const int bottomRightBack = 6;
        protected const int bottomRightFront = 7;


        public int size;

        protected int halfSize;

        public IChunkGroupOrganizer[] children = new IChunkGroupOrganizer[8];

        Vector3Int groupRelativeAnchorPosition;

        public override Vector3Int GroupRelativeAnchorPosition => groupRelativeAnchorPosition;

        protected int GetIndexForLocalPosition(Vector3Int position)
        {
            int result = 0;
            if (position.z >= halfSize) result |= 1;
            if (position.x >= halfSize) result |= 2;
            if (position.y >= halfSize) result |= 4;
            return result;
        }

        protected void GetAnchorPositionForChunkAt(Vector3Int position, out Vector3Int anchorPos, out Vector3Int relAchorPos)
        {
            relAchorPos = new Vector3Int();
            if (position.z >= halfSize) relAchorPos.z += halfSize;
            if (position.x >= halfSize) relAchorPos.x += halfSize;
            if (position.y >= halfSize) relAchorPos.y += halfSize;
            anchorPos = relAchorPos + GroupAnchorPosition;
        }

        public override int Size
        {
            get
            {
                return size;
            }
        }


        public override IMarchingCubeChunk GetChunkAtLocalPosition(Vector3Int pos)
        {
            IChunkGroupOrganizer child = children[GetIndexForLocalPosition(pos)];
            return child.GetChunkAtLocalPosition(pos - child.GroupAnchorPosition);
        }

        public override void SetChunkAtLocalPosition(Vector3Int relativePosition, IMarchingCubeChunk chunk)
        {
            int childIndex = GetIndexForLocalPosition(relativePosition);
            IChunkGroupOrganizer child;

            Vector3Int childAnchorPosition;
            Vector3Int childRelativeAnchorPosition;
            GetAnchorPositionForChunkAt(relativePosition, out childAnchorPosition, out childRelativeAnchorPosition);
            if (chunk.ChunkSize >= halfSize)
            {
                child = new ChunkGroupTreeLeaf(chunk, childAnchorPosition, childRelativeAnchorPosition);
            }
            else
            {
                child = new ChunkGroupTreeNode(childAnchorPosition, childRelativeAnchorPosition, halfSize);
            }
            children[childIndex] = child;
            child.SetChunkAtLocalPosition(child.GroupRelativeAnchorPosition - relativePosition, chunk);
        }

        public override bool TryGetChunkAtLocalPosition(Vector3Int localPosition, out IMarchingCubeChunk chunk)
        {
            IChunkGroupOrganizer child = children[GetIndexForLocalPosition(localPosition)];
            if (child == null)
            {
                chunk = null;
                return false;
            }
            else
            {
                return child.TryGetChunkAtLocalPosition(localPosition - child.GroupRelativeAnchorPosition, out chunk);
            }
        }

        public override bool RemoveChunkAtLocalPosition(Vector3Int relativePosition)
        {
            IChunkGroupOrganizer child = children[GetIndexForLocalPosition(relativePosition)];
            if (child is ChunkGroupTreeLeaf)
            {
                children[GetIndexForLocalPosition(relativePosition)] = null;
                return true;
            }
            else
            {
                return child != null && child.RemoveChunkAtLocalPosition(relativePosition - child.GroupRelativeAnchorPosition);
            }
        }

        public override bool HasChunkAtLocalPosition(Vector3Int relativePosition)
        {
            IChunkGroupOrganizer child = children[GetIndexForLocalPosition(relativePosition)];
            return child != null && 
                (child is ChunkGroupTreeLeaf || child.HasChunkAtLocalPosition(relativePosition - child.GroupRelativeAnchorPosition));

        }
    }
}