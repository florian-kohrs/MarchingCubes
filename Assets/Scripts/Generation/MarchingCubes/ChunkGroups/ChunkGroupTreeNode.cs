using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupTreeNode : IChunkGroupOrganizer
    {

        public ChunkGroupTreeNode(IChunkBuilder builder, Vector3Int anchorPosition, int size)
        {
            Size = size;
            GroupAnchorPosition = anchorPosition;
            ChunkBuilder = builder;
        }

        protected const int topLeftBack = 0;
        protected const int topLeftFront = 1;
        protected const int topRightBack = 2;
        protected const int topRightFront = 3;
        protected const int bottomLeftBack = 4;
        protected const int bottomLeftFront = 5;
        protected const int bottomRightBack = 6;
        protected const int bottomRightFront = 7;

        protected int GetIndexForLocalPosition(Vector3Int position)
        {
            int result = 0;
            if (position.z < halfSize) result |= 1;
            if (position.x < halfSize) result |= 2;
            if (position.y < halfSize) result |= 4;
            return result;
        }

        protected Vector3Int GetNewAnchorPositionForChunkAt(Vector3Int position)
        {
            Vector3Int result = GroupAnchorPosition;
            if (position.z < halfSize) result.z += halfSize;
            if (position.x < halfSize) result.x += halfSize;
            if (position.y < halfSize) result.y += halfSize;
            return result;
        }

        public IChunkGroupOrganizer[] children = new IChunkGroupOrganizer[8];

        public IChunkBuilder ChunkBuilder { get; set; }

        public int Size
        {
            protected set
            {
                size = value;
                halfSize = size / 2;
            }
            get
            {
                return size;
            }
        }

        public int size;

        protected int halfSize;

        public Vector3Int GroupAnchorPosition { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public bool IsEmpty => isEmpty;

        protected bool isEmpty = true;

        public void SetChildAt(IMarchingCubeChunk c, Vector3 pos, int size)
        {

        }

        public IMarchingCubeChunk GetChunkAtLocalPosition(Vector3Int pos)
        {
            IChunkGroupOrganizer child = children[GetIndexForLocalPosition(pos)];
            return child.GetChunkAtLocalPosition(pos - child.GroupAnchorPosition);
        }

        public IMarchingCubeChunk BuildChunkAtLocalPosition(Vector3Int relativePosition, int size, int lodPower)
        {
            int childIndex = GetIndexForLocalPosition(relativePosition);
            IChunkGroupOrganizer child = children[childIndex];

            if (child != null)
                throw new Exception("Chunk group child was already initilized");

            Vector3Int childAnchorPosition = GetNewAnchorPositionForChunkAt(relativePosition);
            if (size >= halfSize)
            {
                child = new ChunkGroupTreeLeaf(ChunkBuilder, childAnchorPosition, size, lodPower);
                children[childIndex] = child;
            }
            else
            {
                child = new ChunkGroupTreeNode(ChunkBuilder, childAnchorPosition, size);
            }
            return child.BuildChunkAtLocalPosition(relativePosition - childAnchorPosition, size, lodPower);
        }

        public bool TryGetChunkAtLocalPosition(Vector3Int localPosition, out IMarchingCubeChunk chunk)
        {
            IChunkGroupOrganizer child = children[GetIndexForLocalPosition(localPosition)];
            if (child == null)
            {
                chunk = null;
                return false;
            }
            else
            {
                return child.TryGetChunkAtLocalPosition(localPosition - child.GroupAnchorPosition, out chunk);
            }
        }

        public bool RemoveChunkAt(Vector3Int relativePosition)
        {
            IChunkGroupOrganizer child = children[GetIndexForLocalPosition(relativePosition)];
            return child != null && child.RemoveChunkAt(relativePosition - child.GroupAnchorPosition);
        }
    }
}