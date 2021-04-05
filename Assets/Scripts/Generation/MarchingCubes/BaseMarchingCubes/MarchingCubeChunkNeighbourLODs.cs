using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public struct MarchingCubeChunkNeighbourLODs
    {
        public int rightNeighbourLod;
        public int leftNeighbourLod;
        public int upperNeighbourLod;
        public int lowerNeighbourLod;
        public int frontNeighbourLod;
        public int backNeighbourLod;

        public static MarchingCubeChunkNeighbourLODs One
        {
            get
            {
                return new MarchingCubeChunkNeighbourLODs(1, 1, 1, 1, 1, 1);
            }
        }

        public int this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return rightNeighbourLod;
                    case 1:
                        return leftNeighbourLod;
                    case 2:
                        return upperNeighbourLod;
                    case 3:
                        return lowerNeighbourLod;
                    case 4:
                        return frontNeighbourLod;
                    default:
                        return backNeighbourLod;
                }
            }
            set
            {
                switch (i)
                {
                    case 0:
                        rightNeighbourLod = value;
                        break;
                    case 1:
                        leftNeighbourLod = value;
                        break;
                    case 2:
                        upperNeighbourLod = value;
                        break;
                    case 3:
                        lowerNeighbourLod = value;
                        break;
                    case 4:
                        frontNeighbourLod = value;
                        break;
                    default:
                        backNeighbourLod = value;
                        break;
                }
            }
        }

        public MarchingCubeChunkNeighbourLODs(int rightNeighbourLod, int leftNeighbourLod, int upperNeighbourLod, int lowerNeighbourLod, int frontNeighbourLod, int backNeighbourLod)
        {
            this.rightNeighbourLod = rightNeighbourLod;
            this.leftNeighbourLod = leftNeighbourLod;
            this.upperNeighbourLod = upperNeighbourLod;
            this.lowerNeighbourLod = lowerNeighbourLod;
            this.frontNeighbourLod = frontNeighbourLod;
            this.backNeighbourLod = backNeighbourLod;
        }
    }
}