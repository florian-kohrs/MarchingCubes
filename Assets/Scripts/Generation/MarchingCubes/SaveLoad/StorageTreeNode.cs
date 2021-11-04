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

        public float[] NoiseMap => Mipmap.vals;

        protected int LOD => (int)Mathf.Pow(2, sizePower - 5);

        protected void CalculateMipMap()
        {
            System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
            w.Start();
            if(mipmap == null)
            {
                mipmap = new StoredChunkEdits();
                mipmap.vals = new float[MIPMAP_SIZE];
                System.Array.Copy(mipmapTemplate, mipmap.vals, MIPMAP_SIZE);
            }
            w.Stop();
            Debug.Log($"Needed time to copy array: {w.Elapsed.TotalMilliseconds}ms" );
            w.Restart();
            for (int i = 0; i < 8; i++)
            {
                var c = children[i];
                if (c != null)
                {
                    CombinePointsInto(c.GroupRelativeAnchorPosition, c.NoiseMap, mipmap.vals, POINTS_PER_AXIS, POINTS_PER_AXIS_SQR, 2, LOD);
                }
            }
            w.Stop();
            Debug.Log($"Needed time to build mipmap: {w.Elapsed.TotalMilliseconds}ms");
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
                relativePosition[0] -= GroupRelativeAnchorPosition[0];
                relativePosition[1] -= GroupRelativeAnchorPosition[1];
                relativePosition[2] -= GroupRelativeAnchorPosition[2];
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

            int startwriteIndex = startIndex[0] / toLod + startIndex[1] / toLod * pointsPerAxis + startIndex[2] / toLod * pointsPerAxisSqr;
            Vector3Int startwriteVec = new Vector3Int
                               (startwriteIndex % pointsPerAxisSqr % pointsPerAxis
                               , startwriteIndex % pointsPerAxisSqr / pointsPerAxis
                               , startwriteIndex / pointsPerAxisSqr
                               );
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
                        int read = readIndex + x;
                        Vector3Int readVec = new Vector3Int
                               (read % pointsPerAxisSqr % pointsPerAxis
                               , read % pointsPerAxisSqr / pointsPerAxis
                               , read / pointsPerAxisSqr
                               );

                        Vector3Int writeVec = new Vector3Int
                             (writeIndex % pointsPerAxisSqr % pointsPerAxis
                             , writeIndex % pointsPerAxisSqr / pointsPerAxis
                             , writeIndex / pointsPerAxisSqr
                             );
                        if(writeIndex >= writeInHere.Length)
                        {

                        }

                        float val = originalPoints[read];
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