using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGroupTreeNode : IChunkLocator
    {

        public ChunkGroupTreeNode(int size)
        {
            this.size = size;
        }

        protected const int topLeftBack = 0;
        protected const int topLeftFront = 1;
        protected const int topRightBack = 2;
        protected const int topRightFront = 3;
        protected const int bottomLeftBack = 4;
        protected const int bottomLeftFront = 5;
        protected const int bottomRightBack = 6;
        protected const int bottomRightFront = 7;

        protected int size;

        public IChunkLocator[] children = new IChunkLocator[8];

        public void SetChildAt(IMarchingCubeChunk c, Vector3 pos, int size)
        {

        }

        public IMarchingCubeChunk GetChunkAtLocal(Vector3Int pos)
        {
            throw new System.NotImplementedException();
        }


    }
}