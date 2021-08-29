using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class MarchingCubeChunkNeighbourLODs
    {
        public MarchingCubeNeighbour rightNeighbourLod;
        public MarchingCubeNeighbour leftNeighbourLod;
        public MarchingCubeNeighbour upperNeighbourLod;
        public MarchingCubeNeighbour lowerNeighbourLod;
        public MarchingCubeNeighbour frontNeighbourLod;
        public MarchingCubeNeighbour backNeighbourLod;

        public bool HasNeighbourWithHigherLOD(int lodPower)
        {
            return rightNeighbourLod.ActiveLodPower > lodPower
                || leftNeighbourLod.ActiveLodPower > lodPower
                || lowerNeighbourLod.ActiveLodPower > lodPower
                || frontNeighbourLod.ActiveLodPower > lodPower
                || backNeighbourLod.ActiveLodPower > lodPower;
        }

        public int GetLodPowerFromNeighbourInDirection(Vector3Int dir)
        {
            if (dir.x > 0)
                return rightNeighbourLod.ActiveLodPower;
            else if (dir.x < 0)
                return leftNeighbourLod.ActiveLodPower;
            else if (dir.y > 0)
                return upperNeighbourLod.ActiveLodPower;
            else if (dir.y < 0)
                return lowerNeighbourLod.ActiveLodPower;
            else if (dir.z > 0)
                return frontNeighbourLod.ActiveLodPower;
            else
                return backNeighbourLod.ActiveLodPower;
            
        }

      

        public MarchingCubeNeighbour this[int i]
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

        public MarchingCubeChunkNeighbourLODs() { }

        public MarchingCubeChunkNeighbourLODs(
            MarchingCubeNeighbour rightNeighbourLod, MarchingCubeNeighbour leftNeighbourLod,
            MarchingCubeNeighbour upperNeighbourLod, MarchingCubeNeighbour lowerNeighbourLod, 
            MarchingCubeNeighbour frontNeighbourLod, MarchingCubeNeighbour backNeighbourLod)
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