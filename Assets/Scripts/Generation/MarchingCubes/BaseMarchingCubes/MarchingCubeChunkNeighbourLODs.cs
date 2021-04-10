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

        public static MarchingCubeChunkNeighbourLODs Two
        {
            get
            {
                return new MarchingCubeChunkNeighbourLODs(2,2,2,2,2,2);
            }
        }


        public bool HasNeighbourWithHigherLOD(int lod)
        {
            return rightNeighbourLod > lod
                || leftNeighbourLod > lod
                || upperNeighbourLod > lod
                || lowerNeighbourLod > lod
                || frontNeighbourLod > lod
                || backNeighbourLod > lod;
        }

        public int GetLodFromNeighbourInDirection(Vector3Int dir)
        {
            if (dir.x > 0)
                return rightNeighbourLod;
            else if (dir.x < 0)
                return leftNeighbourLod;
            else if (dir.y > 0)
                return upperNeighbourLod;
            else if (dir.y < 0)
                return lowerNeighbourLod;
            else if (dir.z > 0)
                return frontNeighbourLod;
            else
                return backNeighbourLod;
            
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