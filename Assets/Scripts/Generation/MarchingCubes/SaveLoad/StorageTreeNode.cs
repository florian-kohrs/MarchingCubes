using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MarchingCubes
{

    [System.Serializable]
    public class StorageTreeNode : GenericTreeNode<StoredChunkEdits, IStorageGroupOrganizer<StoredChunkEdits>, IStorageGroupOrganizer<StoredChunkEdits>>, IStorageGroupOrganizer<StoredChunkEdits>
    {

        public static float NON_SET_NOISE_VALUE = -9999;

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

        //mark dirty when childs noise changes
        protected StoredChunkEdits mipmap;

        protected bool isMipMapComplete;


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

        public float[] NoiseMap => Mipmap.noise;

        protected int LOD => (int)Mathf.Pow(2, sizePower - 5);

        public int ChildrenWithMipMapReady => CountChildrenWithMipmap();

        public int DirectNonNullChildren => CountNonNullChildren();

        public bool HasNoiseMapReady => mipmap != null;

        public bool IsMipMapComplete => isMipMapComplete;

        protected int CountNonNullChildren()
        {
            int result = 0;
            for (int i = 0; i < 8; i++)
            {
                if (children[i] != null)
                    result++;
            }
            return result;
        }

        protected int CountChildrenWithMipmap()
        {
            int result = 0;
            for (int i = 0; i < 8; i++)
            {
                if (children[i] != null && children[i].HasNoiseMapReady)
                    result++;
            }
            return result;
        }


        protected void CalculateMipMap()
        {
            if(mipmap == null)
            {
                mipmap = new StoredChunkEdits();
                mipmap.noise = new float[MIPMAP_SIZE];
                System.Array.Copy(mipmapTemplate, mipmap.noise, MIPMAP_SIZE);
            }
            for (int i = 0; i < 8; i++)
            {
                var c = children[i];
                if (c != null)
                {
                    CombinePointsInto(c.GroupRelativeAnchorPosition, c.NoiseMap, mipmap.noise, POINTS_PER_AXIS, POINTS_PER_AXIS_SQR, 2, LOD);
                }
            }

            //Maybe do async:
            //ThreadPool.GetAvailableThreads(out availableThreads, out availableSyncThreads);
            //if (availableThreads >= 8)
            //{
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done, 0, halfSize, 0, 0, 0, splitThis, frontBotLeft));
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done, 1, halfSize, halfSize, 0, 0, splitThis, frontBotRight));
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done, 2, halfSize, 0, halfSize, 0, splitThis, frontTopLeft));
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done, 3, halfSize, halfSize, halfSize, 0, splitThis, frontTopRight));
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done, 4, halfSize, 0, 0, halfSize, splitThis, backBotLeft));
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done, 5, halfSize, halfSize, 0, halfSize, splitThis, backBotRight));
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done, 6, halfSize, 0, halfSize, halfSize, splitThis, backTopLeft));
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done,7,halfSize, halfSize, halfSize, halfSize, splitThis, backTopRight));
            //}
            //while (done.Contains(false))
            //{

            //}
        }

        public bool TryGetMipMapOfChunkSizePower(int[] relativePosition, int sizePow, out float[] storedNoise, out bool isMipMapComplete)
        {
            if(sizePower == sizePow)
            {
                storedNoise = NoiseMap;
                isMipMapComplete = this.isMipMapComplete;
            }
            else
            {
                relativePosition[0] -= groupRelativeAnchorPosition[0];
                relativePosition[1] -= groupRelativeAnchorPosition[1];
                relativePosition[2] -= groupRelativeAnchorPosition[2];
                int childIndex = GetIndexForLocalPosition(relativePosition);

                if (children[childIndex] == null)
                {
                    storedNoise = null;
                    isMipMapComplete = false;
                }
                else
                {
                    return children[childIndex].TryGetMipMapOfChunkSizePower(relativePosition, sizePow, out storedNoise, out isMipMapComplete);
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

        public bool TryGetNodeWithSizePower(int[] relativePosition, int sizePow, out IStorageGroupOrganizer<StoredChunkEdits> child)
        {
            if (sizePower == sizePow)
            {
                child = this;
            }
            else
            {
                relativePosition[0] -= groupRelativeAnchorPosition[0];
                relativePosition[1] -= groupRelativeAnchorPosition[1];
                relativePosition[2] -= groupRelativeAnchorPosition[2];
                int childIndex = GetIndexForLocalPosition(relativePosition);

                if (children[childIndex] == null)
                {
                    child = null;
                }
                else
                {
                    return children[childIndex].TryGetNodeWithSizePower(relativePosition, sizePow, out child);
                }
            }
            return child != null;
        }

    }

}
