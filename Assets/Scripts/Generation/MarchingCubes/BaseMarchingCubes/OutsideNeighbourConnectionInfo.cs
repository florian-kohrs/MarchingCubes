using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes 
{

    [System.Serializable]
    public struct OutsideNeighbourConnectionInfo
    {

        public int otherTriangleIndex;

        public int outsideNeighbourEdgeIndicesX;
        public int outsideNeighbourEdgeIndicesY;

    }

}
