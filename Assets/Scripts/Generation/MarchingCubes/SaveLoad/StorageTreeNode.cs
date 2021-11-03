using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MarchingCubes
{

    [System.Serializable]
    public class StorageTreeNode : GenericTreeNode<StoredChunkEdits, IStorageGroupOrganizer<StoredChunkEdits>, IStorageGroupOrganizer<StoredChunkEdits>>, IStorageGroupOrganizer<StoredChunkEdits>
    {

        public static int NON_SET_NOISE_VALUE = -9999;

        protected static int POINTS_PER_AXIS = 33;

        protected static int POINTS_PER_AXIS_SQR = POINTS_PER_AXIS * POINTS_PER_AXIS;

        protected static int MIPMAP_SIZE = POINTS_PER_AXIS_SQR * POINTS_PER_AXIS;


        public StorageTreeNode() { }

        public StorageTreeNode(
           int[] anchorPosition,
           int[] relativeAnchorPosition,
           int sizePower) : base(anchorPosition, relativeAnchorPosition, sizePower)
        {
        }

        protected StoredChunkEdits mipmap;

        protected static float[] mipmapTemplate = new float[MIPMAP_SIZE].Fill(NON_SET_NOISE_VALUE);

        protected StoredChunkEdits Mipmap
        {
            get
            {
                if(mipmap == null)
                {
                    CalculateMipMap();
                }
                return mipmap;
            }
        }

        public float[] NoiseMap => Mipmap.vals;

        protected int LOD => (int)Mathf.Pow(2, sizePower - 5);

        protected void CalculateMipMap()
        {
            if(mipmap == null)
            {
                mipmap = new StoredChunkEdits();
                mipmap.vals = new float[MIPMAP_SIZE];
                System.Array.Copy(mipmapTemplate, mipmap.vals, MIPMAP_SIZE);
            }
            for (int i = 0; i < 8; i++)
            {
                var c = children[i];
                if (c != null)
                {
                    CombinePointsInto(c.GroupRelativeAnchorPosition, c.NoiseMap, mipmap.vals, POINTS_PER_AXIS, POINTS_PER_AXIS_SQR, 2, LOD);
                }
            }
        }

        public bool TryGetMipMapOfChunkSizePower(int[] relativePosition, int sizePow, out float[] storedNoise)
        {
            if(sizePower == sizePow)
            {
                storedNoise = NoiseMap;
            }
            else
            {
                relativePosition[0] -= GroupRelativeAnchorPosition[0];
                relativePosition[1] -= GroupRelativeAnchorPosition[1];
                relativePosition[2] -= GroupRelativeAnchorPosition[2];
                int childIndex = GetIndexForLocalPosition(relativePosition);

                if (children[childIndex] == null)
                {
                    storedNoise = null;
                }
                else
                {
                    return children[childIndex].TryGetMipMapOfChunkSizePower(relativePosition, sizePow, out storedNoise);
                }
            }
            return storedNoise != null;
        }

        protected void CombinePointsInto(int[] startIndex, float[] originalPoints, float[] writeInHere, int pointsPerAxis, int pointsPerAxisSqr, int shrinkFactor, int toLod)
        {
            int halfSize = pointsPerAxis / 2;
            int halfSizeCeil = halfSize;
            int halfFrontJump = pointsPerAxis * halfSizeCeil;

            int writeIndex = startIndex[0] / toLod + startIndex[1] / toLod * pointsPerAxis + startIndex[2] / toLod * pointsPerAxisSqr;
            int readIndex;

            for (int z = 0; z < pointsPerAxis; z += shrinkFactor)
            {
                int zPoint = z * pointsPerAxisSqr;
                for (int y = 0; y < pointsPerAxis; y += shrinkFactor)
                {
                    int yPoint = y * pointsPerAxis;
                    readIndex = zPoint + yPoint;
                    for (int x = 0; x < pointsPerAxis; x += shrinkFactor)
                    {
                        float val = originalPoints[readIndex + x];
                        writeInHere[writeIndex] = val;
                        writeIndex++;
                    }
                    writeIndex += halfSizeCeil;
                }
                writeIndex += halfFrontJump;
            }
        }

        public override bool AreAllChildrenLeafs(int targetLodPower)
        {
            return sizePower == MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE_POWER + 1;
        }

        public override IStorageGroupOrganizer<StoredChunkEdits> GetLeaf(StoredChunkEdits leaf, int index, int[] anchor, int[] relAnchor, int sizePow)
        {
            return new StorageTreeLeaf(leaf, index, anchor, relAnchor,sizePow);
        }

        public override IStorageGroupOrganizer<StoredChunkEdits>[] GetLeafs()
        {
            return children;
        }

        public override IStorageGroupOrganizer<StoredChunkEdits> GetNode(int[] anchor, int[] relAnchor, int sizePow)
        {
            return new StorageTreeNode(anchor, relAnchor, sizePow);
        }

    }

}