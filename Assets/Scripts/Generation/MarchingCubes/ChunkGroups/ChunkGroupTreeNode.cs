using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupTreeNode : BaseChunkGroupOrganizer, IChunkGroupParent
    {

        public ChunkGroupTreeNode(
            int[] anchorPosition,
            int[] relativeAnchorPosition, 
            int sizePower)
        {
            this.sizePower = sizePower;
            halfSize = (int)Mathf.Pow(2,sizePower) / 2;
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

        public int sizePower;

        protected int halfSize;

        public IChunkGroupOrganizer[] children = new IChunkGroupOrganizer[8];

        int[] groupRelativeAnchorPosition;

        public override int[] GroupRelativeAnchorPosition => groupRelativeAnchorPosition;

        protected int GetIndexForLocalPosition(int[] position)
        {
            int result = 0;
            if (position[2] >= halfSize) result |= 1;
            if (position[0] >= halfSize) result |= 2;
            if (position[1] >= halfSize) result |= 4;
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

        public override int SizePower
        {
            get
            {
                return sizePower;
            }
        }


        public override IMarchingCubeChunk GetChunkAtLocalPosition(int[] pos)
        {
            IChunkGroupOrganizer child = children[GetIndexForLocalPosition(pos)];
            pos[0] -= GroupAnchorPosition[0];
            pos[1] -= GroupAnchorPosition[1];
            pos[2] -= GroupAnchorPosition[2];
            return child.GetChunkAtLocalPosition(pos);
        }

        public override void SetChunkAtLocalPosition(int[] relativePosition, IMarchingCubeChunk chunk, bool allowOverride)
        {
            relativePosition[0] -= GroupRelativeAnchorPosition[0];
            relativePosition[1] -= GroupRelativeAnchorPosition[1];
            relativePosition[2] -= GroupRelativeAnchorPosition[2];
            int childIndex = GetIndexForLocalPosition(relativePosition);
            
            if (chunk.ChunkSizePower >= sizePower - 1 && (children[childIndex] == null || allowOverride))
            {
                int[] childAnchorPosition;
                int[] childRelativeAnchorPosition;
                GetAnchorPositionForChunkAt(relativePosition, out childAnchorPosition, out childRelativeAnchorPosition);
                children[childIndex] = new ChunkGroupTreeLeaf(this, chunk, childIndex, childAnchorPosition, sizePower - 1);
            }
            else
            {
                IChunkGroupOrganizer child = GetOrCreateChildAt(childIndex, relativePosition);
                //maybe let child substract anchor
                child.SetChunkAtLocalPosition(relativePosition, chunk, allowOverride);
            }
        }

        protected IChunkGroupOrganizer GetOrCreateChildAt(int index, int[] relativePosition)
        {
            if(children[index] == null)
            {
                int[] childAnchorPosition;
                int[] childRelativeAnchorPosition;
                GetAnchorPositionForChunkAt(relativePosition, out childAnchorPosition, out childRelativeAnchorPosition);
                children[index] = new ChunkGroupTreeNode(childAnchorPosition, childRelativeAnchorPosition, sizePower - 1);
            }
            return children[index];
        }

        public override bool TryGetChunkAtLocalPosition(int[] localPosition, out IMarchingCubeChunk chunk)
        {
            localPosition[0] -= GroupRelativeAnchorPosition[0];
            localPosition[1] -= GroupRelativeAnchorPosition[1];
            localPosition[2] -= GroupRelativeAnchorPosition[2];
            IChunkGroupOrganizer child = children[GetIndexForLocalPosition(localPosition)];
            if (child == null)
            {
                chunk = null;
                return false;
            }
            else
            {
                return child.TryGetChunkAtLocalPosition(localPosition, out chunk);
            }
        }

        public override bool RemoveChunkAtLocalPosition(int[] relativePosition)
        {
            relativePosition[0] -= GroupRelativeAnchorPosition[0];
            relativePosition[1] -= GroupRelativeAnchorPosition[1];
            relativePosition[2] -= GroupRelativeAnchorPosition[2];
            IChunkGroupOrganizer child = children[GetIndexForLocalPosition(relativePosition)];
            if (child is ChunkGroupTreeLeaf)
            {
                children[GetIndexForLocalPosition(relativePosition)] = null;
                return true;
            }
            else
            {
                return child != null && child.RemoveChunkAtLocalPosition(relativePosition);
            }
        }

        public override bool HasChunkAtLocalPosition(int[] relativePosition)
        {
            relativePosition[0] -= GroupRelativeAnchorPosition[0];
            relativePosition[1] -= GroupRelativeAnchorPosition[1];
            relativePosition[2] -= GroupRelativeAnchorPosition[2];
            IChunkGroupOrganizer child = children[GetIndexForLocalPosition(relativePosition)];

            return (child == null && (child is ChunkGroupTreeLeaf || child.HasChunkAtLocalPosition(relativePosition)));
        }

        public void SplitChild(ChunkGroupTreeLeaf leaf, int index, IMarchingCubeChunk chunk, IMarchingCubeChunkHandler chunkHandler)
        {
            children[index] = new ChunkGroupTreeNode(GroupAnchorPosition, GroupAnchorPosition, SizePower);
            
        }

        public void Fill(IMarchingCubeChunk chunk, IMarchingCubeChunkHandler chunkHandler)
        {
            float[][] noises = chunkHandler.GetSplittedNoiseArray(chunk);

        }
    }
}