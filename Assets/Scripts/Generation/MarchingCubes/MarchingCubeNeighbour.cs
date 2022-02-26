using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class MarchingCubeNeighbour
    {

        public CompressedMarchingCubeChunk chunk;
        public int estimatedLodPower;
        public bool IsInitialized => chunk != null;
        public int ActiveLodPower
        {
            get
            {
                if (chunk != null)
                    return chunk.LODPower;
                else
                    return estimatedLodPower;
            }
        }

    }

}